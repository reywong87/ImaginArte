using ImaginArte.Application.Rooms;
using ImaginArte.Domain;
using Microsoft.Data.Sqlite;

namespace ImaginArte.Infrastructure.Persistence;

public sealed class SqliteRoomRepository : IRoomRepository, IRoomCleanupRepository
{
    private const int SqliteConstraintErrorCode = 19;
    private readonly string _connectionString;

    public SqliteRoomRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("SQLite connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
        EnsureDatabaseCreated();
    }

    public async Task<Room?> GetByCodeAsync(
        RoomCode code,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT SerializedRoom
            FROM Rooms
            WHERE RoomCode = $roomCode;
            """;
        command.Parameters.AddWithValue("$roomCode", code.Value);

        var serializedRoom = await command.ExecuteScalarAsync(cancellationToken);

        return serializedRoom is string value
            ? RoomJsonSerializer.Deserialize(value)
            : null;
    }

    public async Task AddAsync(
        Room room,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(room);

        var now = DateTimeOffset.UtcNow;
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Rooms (RoomCode, SerializedRoom, CreatedAtUtc, UpdatedAtUtc)
            VALUES ($roomCode, $serializedRoom, $createdAtUtc, $updatedAtUtc);
            """;
        AddRoomParameters(command, room, now, now);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == SqliteConstraintErrorCode)
        {
            throw new RoomAlreadyExistsException(room.Code.Value);
        }
    }

    public async Task SaveAsync(
        Room room,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(room);

        var now = DateTimeOffset.UtcNow;
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Rooms
            SET SerializedRoom = $serializedRoom,
                UpdatedAtUtc = $updatedAtUtc
            WHERE RoomCode = $roomCode;
            """;
        command.Parameters.AddWithValue("$roomCode", room.Code.Value);
        command.Parameters.AddWithValue("$serializedRoom", RoomJsonSerializer.Serialize(room));
        command.Parameters.AddWithValue("$updatedAtUtc", now.ToString("O"));

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

        if (rowsAffected == 0)
        {
            throw new RoomNotFoundException(room.Code.Value);
        }
    }

    public async Task<int> DeleteInactiveRoomsAsync(
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM Rooms
            WHERE UpdatedAtUtc < $cutoffUtc;
            """;
        command.Parameters.AddWithValue("$cutoffUtc", cutoffUtc.ToString("O"));

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void EnsureDatabaseCreated()
    {
        using var connection = CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Rooms (
                RoomCode TEXT NOT NULL PRIMARY KEY,
                SerializedRoom TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    private SqliteConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }

    private static void AddRoomParameters(
        SqliteCommand command,
        Room room,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        command.Parameters.AddWithValue("$roomCode", room.Code.Value);
        command.Parameters.AddWithValue("$serializedRoom", RoomJsonSerializer.Serialize(room));
        command.Parameters.AddWithValue("$createdAtUtc", createdAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$updatedAtUtc", updatedAtUtc.ToString("O"));
    }
}
