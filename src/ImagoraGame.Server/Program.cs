using ImagoraGame.Application.Rooms;
using ImagoraGame.Contracts.Rooms;
using ImagoraGame.Domain;
using ImagoraGame.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.Configure<RoomCleanupOptions>(
    builder.Configuration.GetSection(RoomCleanupOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorDevelopment", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (allowedOrigins.Length > 0)
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});
builder.Services.AddSingleton(typeof(CancellationToken), CancellationToken.None);
var persistenceProvider = builder.Configuration["Persistence:Provider"] ?? "InMemory";
switch (persistenceProvider.ToUpperInvariant())
{
    case "SQLITE":
        builder.Services.AddSingleton(_ => new SqliteRoomRepository(
            builder.Configuration["Persistence:ConnectionString"]
                ?? "Data Source=imagora.db"));
        builder.Services.AddSingleton<IRoomRepository>(
            services => services.GetRequiredService<SqliteRoomRepository>());
        builder.Services.AddSingleton<IRoomCleanupRepository>(
            services => services.GetRequiredService<SqliteRoomRepository>());
        break;
    case "POSTGRESQL":
        var postgresConnectionString = builder.Configuration["Persistence:ConnectionString"];
        if (string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            throw new InvalidOperationException(
                "PostgreSQL persistence requires a non-empty Persistence:ConnectionString. "
                + "Set Persistence__ConnectionString in the deployment environment.");
        }

        builder.Services.AddSingleton(_ => new PostgresRoomRepository(postgresConnectionString));
        builder.Services.AddSingleton<IRoomRepository>(
            services => services.GetRequiredService<PostgresRoomRepository>());
        builder.Services.AddSingleton<IRoomCleanupRepository>(
            services => services.GetRequiredService<PostgresRoomRepository>());
        break;
    case "INMEMORY":
        builder.Services.AddSingleton<IRoomRepository, InMemoryRoomRepository>();
        break;
    default:
        throw new InvalidOperationException(
            $"Unsupported persistence provider '{persistenceProvider}'.");
}

var roomCleanupEnabled = builder.Configuration.GetValue<bool>(
    $"{RoomCleanupOptions.SectionName}:Enabled");
if (roomCleanupEnabled
    && (persistenceProvider.Equals("SQLite", StringComparison.OrdinalIgnoreCase)
        || persistenceProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase)))
{
    builder.Services.AddSingleton<RoomCleanupRunner>();
    builder.Services.AddHostedService<RoomCleanupBackgroundService>();
}
builder.Services.AddTransient<CreateRoomHandler>();
builder.Services.AddTransient<JoinRoomHandler>();
builder.Services.AddTransient<StartGameHandler>();
builder.Services.AddTransient<ChooseStorytellerCardHandler>();
builder.Services.AddTransient<SubmitGuessCardHandler>();
builder.Services.AddTransient<CastVoteHandler>();
builder.Services.AddTransient<ScoreRoundHandler>();
builder.Services.AddTransient<CompleteRoundHandler>();
builder.Services.AddTransient<AdvanceRoundHandler>();
builder.Services.AddTransient<StartNextRoundHandler>();
builder.Services.AddTransient<GetRoomStateHandler>();
builder.Services.AddTransient<GetPlayerPrivateStateHandler>();
builder.Services.AddTransient<RoomRealtimePublisher>();

var app = builder.Build();

app.UseCors("BlazorDevelopment");

app.MapGet("/", () => "Hello World!");
app.MapGet("/health", () => Results.Ok());
app.MapHub<RoomHub>("/hubs/rooms");

app.MapPost("/rooms", async (
    CreateRoomRequest request,
    CreateRoomHandler handler,
    CancellationToken cancellationToken) =>
{
    return await HandleRoomRequestAsync(
        async () => (await handler.HandleAsync(
            new CreateRoomCommand(request.RoomCode),
            cancellationToken)).Room.ToHttpResponse(),
        room => Results.Created($"/rooms/{room.RoomCode}", room));
});

app.MapPost("/rooms/{roomCode}/players", async (
    string roomCode,
    JoinRoomRequest request,
    JoinRoomHandler handler,
    RoomRealtimePublisher realtimePublisher,
    CancellationToken cancellationToken) =>
{
    return await HandleRoomRequestAsync(
        async () =>
        {
            var result = await handler.HandleAsync(
                new JoinRoomCommand(roomCode, request.PlayerId, request.PlayerName),
                cancellationToken);
            var response = result.Room.ToHttpResponse();

            await realtimePublisher.PublishRoomUpdatedAsync(response, cancellationToken);

            return response;
        },
        Results.Ok);
});

app.MapPost("/rooms/{roomCode}/start", async (
    string roomCode,
    StartGameHandler handler,
    GetPlayerPrivateStateHandler getPlayerPrivateState,
    RoomRealtimePublisher realtimePublisher,
    CancellationToken cancellationToken) =>
{
    return await HandleRoomRequestAsync(
        async () =>
        {
            var result = await handler.HandleAsync(
                new StartGameCommand(roomCode),
                cancellationToken);
            var response = result.Room.ToHttpResponse();

            await PublishRoomAndPlayerStatesAsync(
                response,
                getPlayerPrivateState,
                realtimePublisher,
                cancellationToken);

            return response;
        },
        Results.Ok);
});

app.MapPost("/rooms/{roomCode}/storyteller-card", async (
    string roomCode,
    ChooseStorytellerCardRequest request,
    ChooseStorytellerCardHandler handler,
    GetPlayerPrivateStateHandler getPlayerPrivateState,
    RoomRealtimePublisher realtimePublisher,
    CancellationToken cancellationToken) =>
{
    return await HandleRoomRequestAsync(
        async () =>
        {
            var result = await handler.HandleAsync(
                new ChooseStorytellerCardCommand(
                    roomCode,
                    request.PlayerId,
                    request.CardId,
                    request.Clue),
                cancellationToken);
            var response = result.Room.ToHttpResponse();

            await PublishRoomAndPlayerStatesAsync(
                response,
                getPlayerPrivateState,
                realtimePublisher,
                cancellationToken);

            return response;
        },
        Results.Ok);
});

app.MapPost("/rooms/{roomCode}/guess-cards", async (
    string roomCode,
    SubmitGuessCardRequest request,
    SubmitGuessCardHandler handler,
    GetPlayerPrivateStateHandler getPlayerPrivateState,
    RoomRealtimePublisher realtimePublisher,
    CancellationToken cancellationToken) =>
{
    return await HandleRoomRequestAsync(
        async () =>
        {
            var result = await handler.HandleAsync(
                new SubmitGuessCardCommand(
                    roomCode,
                    request.PlayerId,
                    request.CardId),
                cancellationToken);
            var response = result.Room.ToHttpResponse();

            await PublishRoomAndPlayerStatesAsync(
                response,
                getPlayerPrivateState,
                realtimePublisher,
                cancellationToken);

            return response;
        },
        Results.Ok);
});

app.MapPost("/rooms/{roomCode}/votes", async (
    string roomCode,
    CastVoteRequest request,
    CastVoteHandler handler,
    GetPlayerPrivateStateHandler getPlayerPrivateState,
    RoomRealtimePublisher realtimePublisher,
    CancellationToken cancellationToken) =>
{
    return await HandleRoomRequestAsync(
        async () =>
        {
            var result = await handler.HandleAsync(
                new CastVoteCommand(
                    roomCode,
                    request.PlayerId,
                    request.VotingCardId),
                cancellationToken);
            var response = result.Room.ToHttpResponse();

            await PublishRoomAndPlayerStatesAsync(
                response,
                getPlayerPrivateState,
                realtimePublisher,
                cancellationToken);

            return response;
        },
        Results.Ok);
});

app.MapPost("/rooms/{roomCode}/score", async (
    string roomCode,
    ScoreRoundHandler handler,
    GetPlayerPrivateStateHandler getPlayerPrivateState,
    RoomRealtimePublisher realtimePublisher,
    CancellationToken cancellationToken) =>
{
    return await HandleRoomRequestAsync(
        async () =>
        {
            var result = await handler.HandleAsync(
                new ScoreRoundCommand(roomCode),
                cancellationToken);
            var response = result.Room.ToHttpResponse();

            await PublishRoomAndPlayerStatesAsync(
                response,
                getPlayerPrivateState,
                realtimePublisher,
                cancellationToken);

            return response;
        },
        Results.Ok);
});

app.MapPost("/rooms/{roomCode}/complete-round", async (
    string roomCode,
    CompleteRoundHandler handler,
    GetPlayerPrivateStateHandler getPlayerPrivateState,
    RoomRealtimePublisher realtimePublisher,
    CancellationToken cancellationToken) =>
{
    return await HandleRoomRequestAsync(
        async () =>
        {
            var result = await handler.HandleAsync(
                new CompleteRoundCommand(roomCode),
                cancellationToken);
            var response = result.Room.ToHttpResponse();

            await PublishRoomAndPlayerStatesAsync(
                response,
                getPlayerPrivateState,
                realtimePublisher,
                cancellationToken);

            return response;
        },
        Results.Ok);
});

app.MapPost("/rooms/{roomCode}/advance-round", async (
    string roomCode,
    AdvanceRoundHandler handler,
    GetPlayerPrivateStateHandler getPlayerPrivateState,
    RoomRealtimePublisher realtimePublisher,
    CancellationToken cancellationToken) =>
{
    return await HandleRoomRequestAsync(
        async () =>
        {
            var result = await handler.HandleAsync(
                new AdvanceRoundCommand(roomCode),
                cancellationToken);
            var response = result.Room.ToHttpResponse();

            await PublishRoomAndPlayerStatesAsync(
                response,
                getPlayerPrivateState,
                realtimePublisher,
                cancellationToken);

            return response;
        },
        Results.Ok);
});

app.MapPost("/rooms/{roomCode}/next-round", async (
    string roomCode,
    StartNextRoundHandler handler,
    GetPlayerPrivateStateHandler getPlayerPrivateState,
    RoomRealtimePublisher realtimePublisher,
    CancellationToken cancellationToken) =>
{
    return await HandleRoomRequestAsync(
        async () =>
        {
            var result = await handler.HandleAsync(
                new StartNextRoundCommand(roomCode),
                cancellationToken);
            var response = result.Room.ToHttpResponse();

            await PublishRoomAndPlayerStatesAsync(
                response,
                getPlayerPrivateState,
                realtimePublisher,
                cancellationToken);

            return response;
        },
        Results.Ok);
});

app.MapGet("/rooms/{roomCode}", async (
    string roomCode,
    GetRoomStateHandler handler,
    CancellationToken cancellationToken) =>
{
    return await HandleRoomRequestAsync(
        async () => (await handler.HandleAsync(
            new GetRoomStateQuery(roomCode),
            cancellationToken)).Room.ToHttpResponse(),
        Results.Ok);
});

app.MapGet("/rooms/{roomCode}/players/{playerId:guid}", async (
    string roomCode,
    Guid playerId,
    GetPlayerPrivateStateHandler handler,
    CancellationToken cancellationToken) =>
{
    var request = new GetPlayerPrivateStateRequest(roomCode, playerId);

    return await HandleRoomRequestAsync(
        async () => (await handler.HandleAsync(
            new GetPlayerPrivateStateQuery(request.RoomCode, request.PlayerId),
            cancellationToken)).ToHttpResponse(),
        Results.Ok);
});

app.Run();

static async Task<IResult> HandleRoomRequestAsync<TResponse>(
    Func<Task<TResponse>> handle,
    Func<TResponse, IResult> onSuccess)
{
    try
    {
        var response = await handle();
        return onSuccess(response);
    }
    catch (RoomNotFoundException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status404NotFound);
    }
    catch (RoomAlreadyExistsException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status409Conflict);
    }
    catch (PlayerNotFoundInRoomException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status404NotFound);
    }
    catch (DomainRuleViolationException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
    }
}

static async Task PublishRoomAndPlayerStatesAsync(
    RoomStateResponse room,
    GetPlayerPrivateStateHandler getPlayerPrivateState,
    RoomRealtimePublisher realtimePublisher,
    CancellationToken cancellationToken)
{
    await realtimePublisher.PublishRoomUpdatedAsync(room, cancellationToken);

    foreach (var player in room.Players)
    {
        var privateState = await getPlayerPrivateState.HandleAsync(
            new GetPlayerPrivateStateQuery(room.RoomCode, player.PlayerId),
            cancellationToken);

        await realtimePublisher.PublishPlayerStateUpdatedAsync(
            privateState.ToHttpResponse(),
            cancellationToken);
    }
}

public partial class Program;
