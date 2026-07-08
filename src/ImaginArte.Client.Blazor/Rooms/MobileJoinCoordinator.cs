using System.Net;

namespace ImaginArte.Client.Blazor.Rooms;

public sealed class MobileJoinCoordinator
{
    private readonly MobileJoinClient _joinClient;
    private readonly IMobilePlayerSessionStore _sessionStore;
    private readonly Func<Guid> _createPlayerId;

    public MobileJoinCoordinator(
        MobileJoinClient joinClient,
        IMobilePlayerSessionStore sessionStore)
        : this(joinClient, sessionStore, Guid.NewGuid)
    {
    }

    internal MobileJoinCoordinator(
        MobileJoinClient joinClient,
        IMobilePlayerSessionStore sessionStore,
        Func<Guid> createPlayerId)
    {
        _joinClient = joinClient;
        _sessionStore = sessionStore;
        _createPlayerId = createPlayerId;
    }

    public async Task<MobileJoinSessionResult?> TryResumeAsync(
        string roomCode,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionStore.LoadAsync(roomCode);
        if (session is null)
        {
            return null;
        }

        try
        {
            var privateState = await _joinClient.GetPrivateStateAsync(
                session.RoomCode,
                session.PlayerId,
                cancellationToken);

            var lobby = new MobileLobbyState(
                privateState.Room.RoomCode,
                privateState.Player.PlayerId,
                privateState.Player.Name);

            return new MobileJoinSessionResult(lobby, privateState);
        }
        catch (HttpRequestException exception)
            when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            await _sessionStore.ClearAsync(session.RoomCode);
            return null;
        }
    }

    public async Task<MobileJoinSessionResult> JoinNewAsync(
        string roomCode,
        string playerName,
        CancellationToken cancellationToken = default)
    {
        var playerId = _createPlayerId();
        var lobby = await _joinClient.JoinAsync(
            roomCode,
            playerName,
            playerId,
            cancellationToken);

        await _sessionStore.SaveAsync(new MobilePlayerSession(
            lobby.RoomCode,
            lobby.PlayerId,
            lobby.PlayerName));

        return new MobileJoinSessionResult(lobby, PrivateState: null);
    }
}

public sealed record MobileJoinSessionResult(
    MobileLobbyState Lobby,
    ImaginArte.Contracts.Rooms.GetPlayerPrivateStateResponse? PrivateState);
