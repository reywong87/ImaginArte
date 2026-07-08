internal static class RoomHubGroupNames
{
    public static string PublicRoom(string normalizedRoomCode) =>
        $"room:{normalizedRoomCode}";

    public static string PrivatePlayer(string normalizedRoomCode, Guid playerId) =>
        $"player:{normalizedRoomCode}:{playerId}";
}
