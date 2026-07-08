namespace ImaginArte.Client.Blazor.Rooms;

public sealed record MobileLobbyState(
    string RoomCode,
    Guid PlayerId,
    string PlayerName);
