namespace ImagoraGame.Client.Blazor.Rooms;

public sealed record RoomClientOptions(Uri ApiBaseUri, Uri? MobileJoinBaseUri = null);
