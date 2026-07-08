namespace ImaginArte.Contracts.Rooms;

public sealed record RoomUpdatedEvent(
    RoomStateResponse Room);

public sealed record PlayerStateUpdatedEvent(
    GetPlayerPrivateStateResponse State);

public static class RoomRealtimeEventNames
{
    public const string RoomUpdated = "RoomUpdated";
    public const string PlayerStateUpdated = "PlayerStateUpdated";
}
