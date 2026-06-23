using ImagoraGame.Application.Rooms;
using ImagoraGame.Domain;
using ImagoraGame.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IRoomRepository, InMemoryRoomRepository>();
builder.Services.AddTransient<CreateRoomHandler>();
builder.Services.AddTransient<JoinRoomHandler>();
builder.Services.AddTransient<StartGameHandler>();
builder.Services.AddTransient<GetRoomStateHandler>();
builder.Services.AddTransient<GetPlayerPrivateStateHandler>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapPost("/rooms", async (
    CreateRoomRequest request,
    CreateRoomHandler handler,
    CancellationToken cancellationToken) =>
{
    return await HandleRoomRequestAsync(
        async () => (await handler.HandleAsync(
            new CreateRoomCommand(request.RoomCode),
            cancellationToken)).Room,
        room => Results.Created($"/rooms/{room.RoomCode}", room));
});

app.MapPost("/rooms/{roomCode}/players", async (
    string roomCode,
    JoinRoomRequest request,
    JoinRoomHandler handler,
    CancellationToken cancellationToken) =>
{
    return await HandleRoomRequestAsync(
        async () => (await handler.HandleAsync(
            new JoinRoomCommand(roomCode, request.PlayerId, request.PlayerName),
            cancellationToken)).Room,
        Results.Ok);
});

app.MapPost("/rooms/{roomCode}/start", async (
    string roomCode,
    StartGameHandler handler,
    CancellationToken cancellationToken) =>
{
    return await HandleRoomRequestAsync(
        async () => (await handler.HandleAsync(
            new StartGameCommand(roomCode),
            cancellationToken)).Room,
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
            cancellationToken)).Room,
        Results.Ok);
});

app.MapGet("/rooms/{roomCode}/players/{playerId:guid}", async (
    string roomCode,
    Guid playerId,
    GetPlayerPrivateStateHandler handler,
    CancellationToken cancellationToken) =>
{
    return await HandleRoomRequestAsync(
        async () => (await handler.HandleAsync(
            new GetPlayerPrivateStateQuery(roomCode, playerId),
            cancellationToken)).Player,
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

public sealed record CreateRoomRequest(string RoomCode);

public sealed record JoinRoomRequest(Guid PlayerId, string PlayerName);

public partial class Program;
