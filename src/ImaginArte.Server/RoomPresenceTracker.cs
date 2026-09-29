public sealed record RoomPresenceChange(string RoomCode, Guid PlayerId);

public sealed class RoomPresenceTracker
{
    private readonly object _gate = new();
    private readonly Dictionary<RoomPlayerKey, HashSet<string>> _connectionsByPlayer = [];
    private readonly Dictionary<string, HashSet<RoomPlayerKey>> _playersByConnection = [];

    public bool Connect(string roomCode, Guid playerId, string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var key = new RoomPlayerKey(NormalizeRoomCode(roomCode), playerId);

        lock (_gate)
        {
            if (!_connectionsByPlayer.TryGetValue(key, out var connections))
            {
                connections = [];
                _connectionsByPlayer.Add(key, connections);
            }

            var becameConnected = connections.Count == 0;
            if (connections.Add(connectionId))
            {
                if (!_playersByConnection.TryGetValue(connectionId, out var players))
                {
                    players = [];
                    _playersByConnection.Add(connectionId, players);
                }

                players.Add(key);
            }

            return becameConnected;
        }
    }

    public IReadOnlyCollection<RoomPresenceChange> Disconnect(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (_gate)
        {
            if (!_playersByConnection.Remove(connectionId, out var playerKeys))
            {
                return Array.Empty<RoomPresenceChange>();
            }

            var disconnectedPlayers = new List<RoomPresenceChange>();

            foreach (var playerKey in playerKeys)
            {
                if (!_connectionsByPlayer.TryGetValue(playerKey, out var connections))
                {
                    continue;
                }

                connections.Remove(connectionId);

                if (connections.Count == 0)
                {
                    _connectionsByPlayer.Remove(playerKey);
                    disconnectedPlayers.Add(new RoomPresenceChange(
                        playerKey.RoomCode,
                        playerKey.PlayerId));
                }
            }

            return disconnectedPlayers;
        }
    }

    public bool IsConnected(string roomCode, Guid playerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);

        var key = new RoomPlayerKey(NormalizeRoomCode(roomCode), playerId);

        lock (_gate)
        {
            return _connectionsByPlayer.TryGetValue(key, out var connections)
                && connections.Count > 0;
        }
    }

    private static string NormalizeRoomCode(string roomCode) =>
        roomCode.Trim().ToUpperInvariant();

    private readonly record struct RoomPlayerKey(string RoomCode, Guid PlayerId);
}
