using ImaginArte.Application.Rooms;
using ImaginArte.Contracts.Rooms;
using ImaginArte.Domain;
using ImaginArte.Infrastructure.Persistence;

namespace ImaginArte.Infrastructure.Tests;

public sealed class SqliteRoomRepositoryTests
{
    private const string LegacyRoomJson = """
        {
          "roomCode": "OLD123",
          "phase": "WaitingForPlayers",
          "targetScore": 25,
          "currentStorytellerId": null,
          "storytellerCardId": null,
          "clue": null,
          "players": [
            { "playerId": "b8a95cef-711d-42f8-ae45-183922f542c6", "name": "Alice", "hand": [] }
          ],
          "availableDeck": [],
          "discardPile": [],
          "guessCardSubmissions": [],
          "votingCardOwnerships": [],
          "votes": [],
          "scoreboard": [],
          "roundScores": []
        }
        """;

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
        room.Join(playerId, "Alice", AvatarId.Fox);
        await repository.AddAsync(room);

        var savedRoom = await repository.GetByCodeAsync(new RoomCode("abc123"));

        Assert.NotNull(savedRoom);
        Assert.Equal("ABC123", savedRoom.Code.Value);
        var player = Assert.Single(savedRoom.Players);
        Assert.Equal(playerId, player.Id);
        Assert.Equal("Alice", player.Name);
        Assert.Equal(AvatarId.Fox, player.AvatarId);
    }

    [Fact]
    public async Task GetByCodeAsync_WhenStoredRoomUsesPreviousSchema_ReturnsNull()
    {
        await using var database = TemporarySqliteDatabase.Create();
        var repository = database.CreateRepository();
        await repository.AddAsync(Room.Create(new RoomCode("OLD123")));
        await database.SetSerializedRoomAsync("OLD123", LegacyRoomJson);

        var room = await repository.GetByCodeAsync(new RoomCode("OLD123"));

        Assert.Null(room);
    }

    [Fact]
    public async Task GetByCodeAsync_PreservesPlayerAvatarFromVersionedDocument()
    {
        await using var database = TemporarySqliteDatabase.Create();
        var repository = database.CreateRepository();
        var room = Room.Create(new RoomCode("ABC123"));
        room.Join(PlayerId.New(), "Alice", AvatarId.Snail);
        await repository.AddAsync(room);

        var savedRoom = await repository.GetByCodeAsync(new RoomCode("ABC123"));

        Assert.NotNull(savedRoom);
        Assert.Equal(AvatarId.Snail, Assert.Single(savedRoom.Players).AvatarId);
    }

    [Fact]
    public async Task SaveAsync_WhenRoomExists_UpdatesRoom()
    {
        await using var database = TemporarySqliteDatabase.Create();
        var repository = database.CreateRepository();
        var room = Room.Create(new RoomCode("ABC123"));
        await repository.AddAsync(room);
        var playerId = PlayerId.New();

        room.Join(playerId, "Alice", AvatarId.Fox);
        room.SetTargetScore(35);
        await repository.SaveAsync(room);

        var savedRoom = await repository.GetByCodeAsync(new RoomCode("ABC123"));
        Assert.NotNull(savedRoom);
        Assert.Equal(35, savedRoom.TargetScore);
        var player = Assert.Single(savedRoom.Players);
        Assert.Equal(playerId, player.Id);
        Assert.Equal("Alice", player.Name);
        Assert.Equal(AvatarId.Fox, player.AvatarId);
    }

    [Fact]
    public async Task VoteRevealPhase_PersistsAcrossRepositoryInstances()
    {
        await using var database = TemporarySqliteDatabase.Create();
        var firstRepository = database.CreateRepository();
        var room = Room.Create(new RoomCode("ABC123"));
        var aliceId = PlayerId.New();
        var bobId = PlayerId.New();
        var carlaId = PlayerId.New();
        room.Join(aliceId, "Alice", AvatarId.Fox);
        room.Join(bobId, "Bob", AvatarId.Owl);
        room.Join(carlaId, "Carla", AvatarId.Deer);
        room.StartGame();

        var storytellerId = room.CurrentStorytellerId
            ?? throw new InvalidOperationException("Test setup requires a storyteller.");
        room.ChooseStorytellerCard(storytellerId, room.GetHandFor(storytellerId).First().Id, "A clue");

        foreach (var guesser in room.Players.Where(player => player.Id != storytellerId))
        {
            room.SubmitGuessCard(guesser.Id, room.GetHandFor(guesser.Id).First().Id);
        }

        var storytellerVotingCardId = room.VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;
        var guessers = room.Players.Where(player => player.Id != storytellerId).ToArray();
        room.CastVote(guessers[0].Id, storytellerVotingCardId);
        room.CastVote(guessers[1].Id, storytellerVotingCardId);

        Assert.Equal(GamePhase.VoteReveal, room.Phase);
        await firstRepository.AddAsync(room);

        var secondRepository = database.CreateRepository();
        var savedRoom = await secondRepository.GetByCodeAsync(new RoomCode("ABC123"));

        Assert.NotNull(savedRoom);
        Assert.Equal(GamePhase.VoteReveal, savedRoom.Phase);
        Assert.Equal(2, savedRoom.VoteCount);
        Assert.Equal(
            room.VotingCards
                .SelectMany(card => card.Votes.Select(vote => (
                    VotingCardId: card.VotingCardId.Value,
                    VoterId: vote.VoterPlayerId.Value)))
                .OrderBy(vote => vote.VotingCardId)
                .ThenBy(vote => vote.VoterId),
            savedRoom.VotingCards
                .SelectMany(card => card.Votes.Select(vote => (
                    VotingCardId: card.VotingCardId.Value,
                    VoterId: vote.VoterPlayerId.Value)))
                .OrderBy(vote => vote.VotingCardId)
                .ThenBy(vote => vote.VoterId));
        Assert.Empty(savedRoom.RevealedVotingCards);
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
        room.Join(playerId, "Alice", AvatarId.Fox);
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
        await using var database = TemporarySqliteDatabase.Create();
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
        await using var database = TemporarySqliteDatabase.Create();
        var repository = database.CreateRepository();
        await repository.AddAsync(Room.Create(new RoomCode("NEW123")));
        var cutoffUtc = DateTimeOffset.Parse("2026-01-02T00:00:00Z");
        await database.SetUpdatedAtUtcAsync("NEW123", cutoffUtc);

        var deletedRooms = await repository.DeleteInactiveRoomsAsync(cutoffUtc);

        Assert.Equal(0, deletedRooms);
        Assert.NotNull(await repository.GetByCodeAsync(new RoomCode("NEW123")));
    }

    [Fact]
    public void CoreAssemblies_DoNotDependOnPersistenceTechnologyOrInfrastructure()
    {
        var forbiddenDependencyNames = new[]
        {
            "ImaginArte.Infrastructure",
            "Microsoft.Data.Sqlite",
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
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

        public async Task SetUpdatedAtUtcAsync(
            string roomCode,
            DateTimeOffset updatedAtUtc)
        {
            await using var connection = new Microsoft.Data.Sqlite.SqliteConnection(
                $"Data Source={Path}");
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE Rooms
                SET UpdatedAtUtc = $updatedAtUtc
                WHERE RoomCode = $roomCode;
                """;
            command.Parameters.AddWithValue("$updatedAtUtc", updatedAtUtc.ToString("O"));
            command.Parameters.AddWithValue("$roomCode", roomCode);

            await command.ExecuteNonQueryAsync();
        }

        public async Task SetSerializedRoomAsync(string roomCode, string serializedRoom)
        {
            await using var connection = new Microsoft.Data.Sqlite.SqliteConnection(
                $"Data Source={Path}");
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Rooms SET SerializedRoom = $serializedRoom WHERE RoomCode = $roomCode;";
            command.Parameters.AddWithValue("$serializedRoom", serializedRoom);
            command.Parameters.AddWithValue("$roomCode", roomCode);

            await command.ExecuteNonQueryAsync();
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
