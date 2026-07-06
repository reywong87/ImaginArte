namespace ImagoraGame.Client.Blazor.Rooms;

public interface IMobilePlayerSessionStore
{
    ValueTask<MobilePlayerSession?> LoadAsync(string roomCode);

    ValueTask SaveAsync(MobilePlayerSession session);

    ValueTask ClearAsync(string roomCode);
}
