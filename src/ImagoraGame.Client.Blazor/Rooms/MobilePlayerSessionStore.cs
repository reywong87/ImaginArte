using System.Text.Json;

namespace ImagoraGame.Client.Blazor.Rooms;

public sealed class MobilePlayerSessionStore(IBrowserStorage storage)
    : IMobilePlayerSessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<MobilePlayerSession?> LoadAsync(string roomCode)
    {
        var normalizedRoomCode = NormalizeRoomCode(roomCode);
        var storedJson = await storage.GetItemAsync(GetStorageKey(normalizedRoomCode));
        if (string.IsNullOrWhiteSpace(storedJson))
        {
            return null;
        }

        StoredMobilePlayerSession? storedSession;
        try
        {
            storedSession = JsonSerializer.Deserialize<StoredMobilePlayerSession>(
                storedJson,
                JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (storedSession is null ||
            string.IsNullOrWhiteSpace(storedSession.RoomCode) ||
            !string.Equals(
                NormalizeRoomCode(storedSession.RoomCode),
                normalizedRoomCode,
                StringComparison.Ordinal) ||
            !Guid.TryParse(storedSession.PlayerId, out var playerId) ||
            string.IsNullOrWhiteSpace(storedSession.PlayerName))
        {
            return null;
        }

        return new MobilePlayerSession(
            normalizedRoomCode,
            playerId,
            storedSession.PlayerName.Trim());
    }

    public async ValueTask SaveAsync(MobilePlayerSession session)
    {
        var normalizedRoomCode = NormalizeRoomCode(session.RoomCode);
        var storedSession = new StoredMobilePlayerSession(
            normalizedRoomCode,
            session.PlayerId.ToString("D"),
            session.PlayerName.Trim());

        var storedJson = JsonSerializer.Serialize(storedSession, JsonOptions);
        await storage.SetItemAsync(GetStorageKey(normalizedRoomCode), storedJson);
    }

    public ValueTask ClearAsync(string roomCode)
    {
        return storage.RemoveItemAsync(GetStorageKey(NormalizeRoomCode(roomCode)));
    }

    internal static string GetStorageKey(string roomCode)
    {
        return $"imagora.rooms.{NormalizeRoomCode(roomCode)}.player";
    }

    private static string NormalizeRoomCode(string roomCode)
    {
        return roomCode.Trim().ToUpperInvariant();
    }

    private sealed record StoredMobilePlayerSession(
        string? RoomCode,
        string? PlayerId,
        string? PlayerName);
}
