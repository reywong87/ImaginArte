namespace ImaginArte.Client.Blazor.Rooms;

public sealed record MobilePlayerSession(
    string RoomCode,
    Guid PlayerId,
    string PlayerName);
