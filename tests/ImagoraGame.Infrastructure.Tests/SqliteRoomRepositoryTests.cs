using ImagoraGame.Application.Rooms;
using ImagoraGame.Contracts.Rooms;
using ImagoraGame.Domain;
using ImagoraGame.Infrastructure.Persistence;

namespace ImagoraGame.Infrastructure.Tests;

public sealed class SqliteRoomRepositoryTests
{
    [Fact]
    public async Task AddAsync_SavesRoom()
    {
        await using var database = TemporarySqliteDatabase.Create();
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
        await using var database = TemporarySqliteDatabase.Create();
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
        await using var database = TemporarySqliteDatabase.Create();
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
        await using var database = TemporarySqliteDatabase.Create();
        var repository = database.CreateRepository();
        await repository.AddAsync(Room.Create(new RoomCode("ABC123")));

        var exception = await Assert.ThrowsAsync<RoomAlreadyExistsException>(
            () => repository.AddAsync(Room.Create(new RoomCode("abc123"))));

        Assert.Equal("ABC123", exception.RoomCode);
    }

    [Fact]
    public async Task GetByCodeAsync_WhenRoomDoesNotExist_ReturnsNull()
    {
        await using var database = TemporarySqliteDatabase.Create();
        var repository = database.CreateRepository();

        var room = await repository.GetByCodeAsync(new RoomCode("missing"));

        Assert.Null(room);
    }

    [Fact]
    public async Task SaveAsync_WhenRoomDoesNotExist_Throws()
    {
        await using var database = TemporarySqliteDatabase.Create();
        var repository = database.CreateRepository();
        var room = Room.Create(new RoomCode("missing"));

        var exception = await Assert.ThrowsAsync<RoomNotFoundException>(
            () => repository.SaveAsync(room));

        Assert.Equal("MISSING", exception.RoomCode);
    }

    [Fact]
    public async Task RoomPersistsAcrossRepositoryInstances()
    {
        await using var database = TemporarySqliteDatabase.Create();
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
    public void CoreAssemblies_DoNotDependOnPersistenceTechnologyOrInfrastructure()
    {
        var forbiddenDependencyNames = new[]
        {
            "ImagoraGame.Infrastructure",
            "Microsoft.Data.Sqlite",
            "Microsoft.EntityFrameworkCore",
            "SQLitePCLRaw"
        };
        var assemblies = new[]
        {
            typeof(Room).Assembly,
            typeof(IRoomRepository).Assembly,
            typeof(RoomUpdatedEvent).Assembly
        };

        foreach (var assembly in assemblies)
        {
            var referencedAssemblyNames = assembly
                .GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .Where(name => name is not null)
                .Cast<string>();

            Assert.DoesNotContain(
                referencedAssemblyNames,
                reference => forbiddenDependencyNames.Any(
                    forbidden => reference.Equals(forbidden, StringComparison.Ordinal)
                        || reference.StartsWith($"{forbidden}.", StringComparison.Ordinal)));
        }
    }

    private sealed class TemporarySqliteDatabase : IAsyncDisposable
    {
        private TemporarySqliteDatabase(string path)
        {
            Path = path;
        }

        private string Path { get; }

        public static TemporarySqliteDatabase Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"imagora-{Guid.NewGuid():N}.db");

            return new TemporarySqliteDatabase(path);
        }

        public SqliteRoomRepository CreateRepository()
        {
            return new SqliteRoomRepository($"Data Source={Path}");
        }

        public ValueTask DisposeAsync()
        {
            DeleteIfExists(Path);
            DeleteIfExists($"{Path}-shm");
            DeleteIfExists($"{Path}-wal");

            return ValueTask.CompletedTask;
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
