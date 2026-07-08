using ImaginArte.Application.Rooms;
using ImaginArte.Domain;
using ImaginArte.Infrastructure.Persistence;
using Npgsql;
using Testcontainers.PostgreSql;

namespace ImaginArte.Infrastructure.Tests;

public sealed class PostgresRoomRepositoryTests
    : IClassFixture<PostgresRoomRepositoryTests.PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public PostgresRoomRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_SavesRoom()
    {
        await using var database = await TemporaryPostgresDatabase.CreateAsync(_fixture.ConnectionString);
        var repository = database.CreateRepository();
        var room = Room.Create(new RoomCode("ABC123"));

        await repository.AddAsync(room);

        var savedRoom = await repository.GetByCodeAsync(new RoomCode("ABC123"));
        Assert.NotNull(savedRoom);
        Assert.Equal("ABC123", savedRoom.Code.Value);
    }

    [Fact]
    public async Task GetByCodeAsync_WhenRoomExists_ReturnsRoom()
    {
        await using var database = await TemporaryPostgresDatabase.CreateAsync(_fixture.ConnectionString);
        var repository = database.CreateRepository();
        var room = Room.Create(new RoomCode("ABC123"));
        var playerId = PlayerId.New();
        room.Join(playerId, "Alice");
        await repository.AddAsync(room);

        var savedRoom = await repository.GetByCodeAsync(new RoomCode("abc123"));

        Assert.NotNull(savedRoom);
        Assert.Equal("ABC123", savedRoom.Code.Value);
        var player = Assert.Single(savedRoom.Players);
        Assert.Equal(playerId, player.Id);
        Assert.Equal("Alice", player.Name);
    }

    [Fact]
    public async Task SaveAsync_WhenRoomExists_UpdatesRoom()
    {
        await using var database = await TemporaryPostgresDatabase.CreateAsync(_fixture.ConnectionString);
        var repository = database.CreateRepository();
        var room = Room.Create(new RoomCode("ABC123"));
        await repository.AddAsync(room);
        var playerId = PlayerId.New();

        room.Join(playerId, "Alice");
        await repository.SaveAsync(room);

        var savedRoom = await repository.GetByCodeAsync(new RoomCode("ABC123"));
        Assert.NotNull(savedRoom);
        var player = Assert.Single(savedRoom.Players);
        Assert.Equal(playerId, player.Id);
        Assert.Equal("Alice", player.Name);
    }

    [Fact]
    public async Task AddAsync_WhenRoomCodeAlreadyExists_Throws()
    {
        await using var database = await TemporaryPostgresDatabase.CreateAsync(_fixture.ConnectionString);
        var repository = database.CreateRepository();
        await repository.AddAsync(Room.Create(new RoomCode("ABC123")));

        var exception = await Assert.ThrowsAsync<RoomAlreadyExistsException>(
            () => repository.AddAsync(Room.Create(new RoomCode("abc123"))));

        Assert.Equal("ABC123", exception.RoomCode);
    }

    [Fact]
    public async Task GetByCodeAsync_WhenRoomDoesNotExist_ReturnsNull()
    {
        await using var database = await TemporaryPostgresDatabase.CreateAsync(_fixture.ConnectionString);
        var repository = database.CreateRepository();

        var room = await repository.GetByCodeAsync(new RoomCode("missing"));

        Assert.Null(room);
    }

    [Fact]
    public async Task SaveAsync_WhenRoomDoesNotExist_Throws()
    {
        await using var database = await TemporaryPostgresDatabase.CreateAsync(_fixture.ConnectionString);
        var repository = database.CreateRepository();
        var room = Room.Create(new RoomCode("missing"));

        var exception = await Assert.ThrowsAsync<RoomNotFoundException>(
            () => repository.SaveAsync(room));

        Assert.Equal("MISSING", exception.RoomCode);
    }

    [Fact]
    public async Task RoomPersistsAcrossRepositoryInstances()
    {
        await using var database = await TemporaryPostgresDatabase.CreateAsync(_fixture.ConnectionString);
        var firstRepository = database.CreateRepository();
        var room = Room.Create(new RoomCode("ABC123"));
        var playerId = PlayerId.New();
        room.Join(playerId, "Alice");
        await firstRepository.AddAsync(room);

        var secondRepository = database.CreateRepository();
        var savedRoom = await secondRepository.GetByCodeAsync(new RoomCode("ABC123"));

        Assert.NotNull(savedRoom);
        Assert.Equal("ABC123", savedRoom.Code.Value);
        var player = Assert.Single(savedRoom.Players);
        Assert.Equal(playerId, player.Id);
        Assert.Equal("Alice", player.Name);
    }

    [Fact]
    public async Task DeleteInactiveRoomsAsync_DeletesOnlyRoomsOlderThanCutoff()
    {
        await using var database = await TemporaryPostgresDatabase.CreateAsync(_fixture.ConnectionString);
        var repository = database.CreateRepository();
        await repository.AddAsync(Room.Create(new RoomCode("OLD123")));
        await repository.AddAsync(Room.Create(new RoomCode("NEW123")));
        var cutoffUtc = DateTimeOffset.Parse("2026-01-02T00:00:00Z");
        await database.SetUpdatedAtUtcAsync("OLD123", cutoffUtc.AddMinutes(-1));
        await database.SetUpdatedAtUtcAsync("NEW123", cutoffUtc.AddMinutes(1));

        var deletedRooms = await repository.DeleteInactiveRoomsAsync(cutoffUtc);

        Assert.Equal(1, deletedRooms);
        Assert.Null(await repository.GetByCodeAsync(new RoomCode("OLD123")));
        Assert.NotNull(await repository.GetByCodeAsync(new RoomCode("NEW123")));
    }

    [Fact]
    public async Task DeleteInactiveRoomsAsync_DoesNotDeleteRoomsNewerThanCutoff()
    {
        await using var database = await TemporaryPostgresDatabase.CreateAsync(_fixture.ConnectionString);
        var repository = database.CreateRepository();
        await repository.AddAsync(Room.Create(new RoomCode("NEW123")));
        var cutoffUtc = DateTimeOffset.Parse("2026-01-02T00:00:00Z");
        await database.SetUpdatedAtUtcAsync("NEW123", cutoffUtc);

        var deletedRooms = await repository.DeleteInactiveRoomsAsync(cutoffUtc);

        Assert.Equal(0, deletedRooms);
        Assert.NotNull(await repository.GetByCodeAsync(new RoomCode("NEW123")));
    }

    public sealed class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("imagora_tests")
            .WithUsername("imagora")
            .WithPassword("imagora")
            .Build();

        public string ConnectionString => _container.GetConnectionString();

        public Task InitializeAsync()
        {
            return _container.StartAsync();
        }

        public Task DisposeAsync()
        {
            return _container.DisposeAsync().AsTask();
        }
    }

    private sealed class TemporaryPostgresDatabase : IAsyncDisposable
    {
        private readonly string _adminConnectionString;

        private TemporaryPostgresDatabase(
            string adminConnectionString,
            string schemaName,
            string repositoryConnectionString)
        {
            _adminConnectionString = adminConnectionString;
            SchemaName = schemaName;
            RepositoryConnectionString = repositoryConnectionString;
        }

        private string SchemaName { get; }

        private string RepositoryConnectionString { get; }

        public static async Task<TemporaryPostgresDatabase> CreateAsync(
            string adminConnectionString)
        {
            var schemaName = $"imagora_{Guid.NewGuid():N}";
            await using var connection = new NpgsqlConnection(adminConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                $"""
                CREATE SCHEMA "{schemaName}";
                """,
                connection);
            await command.ExecuteNonQueryAsync();

            var repositoryConnectionStringBuilder = new NpgsqlConnectionStringBuilder(adminConnectionString)
            {
                SearchPath = schemaName
            };

            return new TemporaryPostgresDatabase(
                adminConnectionString,
                schemaName,
                repositoryConnectionStringBuilder.ConnectionString);
        }

        public PostgresRoomRepository CreateRepository()
        {
            return new PostgresRoomRepository(RepositoryConnectionString);
        }

        public async Task SetUpdatedAtUtcAsync(
            string roomCode,
            DateTimeOffset updatedAtUtc)
        {
            await using var connection = new NpgsqlConnection(RepositoryConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                """
                UPDATE "Rooms"
                SET "UpdatedAtUtc" = @updatedAtUtc
                WHERE "RoomCode" = @roomCode;
                """,
                connection);
            command.Parameters.AddWithValue("updatedAtUtc", updatedAtUtc);
            command.Parameters.AddWithValue("roomCode", roomCode);

            await command.ExecuteNonQueryAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await using var connection = new NpgsqlConnection(_adminConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                $"""
                DROP SCHEMA IF EXISTS "{SchemaName}" CASCADE;
                """,
                connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
