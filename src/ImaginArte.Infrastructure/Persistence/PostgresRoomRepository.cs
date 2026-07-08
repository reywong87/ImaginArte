using ImaginArte.Application.Rooms;
using ImaginArte.Domain;
using Npgsql;

namespace ImaginArte.Infrastructure.Persistence;

public sealed class PostgresRoomRepository : IRoomRepository, IRoomCleanupRepository
{
    private const string UniqueViolationSqlState = "23505";
    private readonly string _connectionString;
    private readonly SemaphoreSlim _databaseCreationGate = new(1, 1);
    private bool _databaseCreated;

    public PostgresRoomRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("PostgreSQL connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public async Task<Room?> GetByCodeAsync(
        RoomCode code,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);
        await EnsureDatabaseCreatedAsync(cancellationToken);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT "SerializedRoom"
            FROM "Rooms"
            WHERE "RoomCode" = @roomCode;
            """,
            connection);
        command.Parameters.AddWithValue("roomCode", code.Value);

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
        await EnsureDatabaseCreatedAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO "Rooms" ("RoomCode", "SerializedRoom", "CreatedAtUtc", "UpdatedAtUtc")
            VALUES (@roomCode, @serializedRoom, @createdAtUtc, @updatedAtUtc);
            """,
            connection);
        AddRoomParameters(command, room, now, now);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException exception) when (exception.SqlState == UniqueViolationSqlState)
        {
            throw new RoomAlreadyExistsException(room.Code.Value);
        }
    }

    public async Task SaveAsync(
        Room room,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(room);
        await EnsureDatabaseCreatedAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE "Rooms"
            SET "SerializedRoom" = @serializedRoom,
                "UpdatedAtUtc" = @updatedAtUtc
            WHERE "RoomCode" = @roomCode;
            """,
            connection);
        command.Parameters.AddWithValue("roomCode", room.Code.Value);
        command.Parameters.AddWithValue("serializedRoom", RoomJsonSerializer.Serialize(room));
        command.Parameters.AddWithValue("updatedAtUtc", now);

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
        await EnsureDatabaseCreatedAsync(cancellationToken);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            DELETE FROM "Rooms"
            WHERE "UpdatedAtUtc" < @cutoffUtc;
            """,
            connection);
        command.Parameters.AddWithValue("cutoffUtc", cutoffUtc);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken)
    {
        if (_databaseCreated)
        {
            return;
        }

        await _databaseCreationGate.WaitAsync(cancellationToken);
        try
        {
            if (_databaseCreated)
            {
                return;
            }

            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                """
                CREATE TABLE IF NOT EXISTS "Rooms" (
                    "RoomCode" text NOT NULL PRIMARY KEY,
                    "SerializedRoom" text NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "UpdatedAtUtc" timestamp with time zone NOT NULL
                );
                """,
                connection);
            await command.ExecuteNonQueryAsync(cancellationToken);

            _databaseCreated = true;
        }
        finally
        {
            _databaseCreationGate.Release();
        }
    }

    private NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    private static void AddRoomParameters(
        NpgsqlCommand command,
        Room room,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        command.Parameters.AddWithValue("roomCode", room.Code.Value);
        command.Parameters.AddWithValue("serializedRoom", RoomJsonSerializer.Serialize(room));
        command.Parameters.AddWithValue("createdAtUtc", createdAtUtc);
        command.Parameters.AddWithValue("updatedAtUtc", updatedAtUtc);
    }
}
