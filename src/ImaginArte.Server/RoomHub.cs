using ImaginArte.Application.Rooms;
using ImaginArte.Contracts.Rooms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

public sealed class RoomHub : Hub
{
    private readonly GetRoomStateHandler _getRoomState;
    private readonly GetPlayerPrivateStateHandler _getPlayerPrivateState;
    private readonly RoomPresenceTracker _presenceTracker;
    private readonly RoomRealtimePublisher _realtimePublisher;
    private readonly ILogger<RoomHub> _logger;

    public RoomHub(
        GetRoomStateHandler getRoomState,
        GetPlayerPrivateStateHandler getPlayerPrivateState,
        RoomPresenceTracker presenceTracker,
        RoomRealtimePublisher realtimePublisher,
        ILogger<RoomHub> logger)
    {
        _getRoomState = getRoomState;
        _getPlayerPrivateState = getPlayerPrivateState;
        _presenceTracker = presenceTracker;
        _realtimePublisher = realtimePublisher;
        _logger = logger;
    }

    public async Task SubscribeToRoom(
        string roomCode,
        [FromServices] CancellationToken cancellationToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            Context.ConnectionAborted);

        try
        {
            var result = await _getRoomState.HandleAsync(
                new GetRoomStateQuery(roomCode),
                cancellation.Token);
            var room = result.Room.ToHttpResponse(_presenceTracker);

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                RoomHubGroupNames.PublicRoom(room.RoomCode),
                cancellation.Token);
            await Clients.Caller.SendAsync(
                RoomRealtimeEventNames.RoomUpdated,
                new RoomUpdatedEvent(room),
                cancellation.Token);
        }
        catch (RoomNotFoundException exception)
        {
            throw new HubException($"Room '{exception.RoomCode}' was not found.");
        }
        catch (Exception exception) when (exception is not HubException)
        {
            _logger.LogError(
                exception,
                "Unexpected error subscribing connection {ConnectionId} to room.",
                Context.ConnectionId);
            throw new HubException("The room subscription could not be completed.");
        }
    }

    public async Task SubscribeAsPlayer(
        string roomCode,
        Guid playerId,
        [FromServices] CancellationToken cancellationToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            Context.ConnectionAborted);

        try
        {
            var result = await _getPlayerPrivateState.HandleAsync(
                new GetPlayerPrivateStateQuery(roomCode, playerId),
                cancellation.Token);
            var normalizedRoomCode = result.Room.RoomCode;

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                RoomHubGroupNames.PublicRoom(normalizedRoomCode),
                cancellation.Token);
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                RoomHubGroupNames.PrivatePlayer(normalizedRoomCode, playerId),
                cancellation.Token);

            var becameConnected = _presenceTracker.Connect(
                normalizedRoomCode,
                playerId,
                Context.ConnectionId);
            var state = result.ToHttpResponse(_presenceTracker);

            if (becameConnected)
            {
                await _realtimePublisher.PublishRoomUpdatedAsync(state.Room, cancellation.Token);
            }
            else
            {
                await Clients.Caller.SendAsync(
                    RoomRealtimeEventNames.RoomUpdated,
                    new RoomUpdatedEvent(state.Room),
                    cancellation.Token);
            }

            await Clients.Caller.SendAsync(
                RoomRealtimeEventNames.PlayerStateUpdated,
                new PlayerStateUpdatedEvent(state),
                cancellation.Token);
        }
        catch (RoomNotFoundException exception)
        {
            throw new HubException($"Room '{exception.RoomCode}' was not found.");
        }
        catch (PlayerNotFoundInRoomException exception)
        {
            throw new HubException(
                $"Player '{exception.PlayerId}' was not found in room '{exception.RoomCode}'.");
        }
        catch (Exception exception) when (exception is not HubException)
        {
            _logger.LogError(
                exception,
                "Unexpected error subscribing connection {ConnectionId} as a player.",
                Context.ConnectionId);
            throw new HubException("The player subscription could not be completed.");
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            foreach (var disconnectedPlayer in _presenceTracker.Disconnect(Context.ConnectionId))
            {
                try
                {
                    var result = await _getRoomState.HandleAsync(
                        new GetRoomStateQuery(disconnectedPlayer.RoomCode));
                    var room = result.Room.ToHttpResponse(_presenceTracker);

                    await _realtimePublisher.PublishRoomUpdatedAsync(room);
                }
                catch (RoomNotFoundException roomNotFoundException)
                {
                    _logger.LogInformation(
                        roomNotFoundException,
                        "Room {RoomCode} disappeared while disconnecting player {PlayerId}.",
                        disconnectedPlayer.RoomCode,
                        disconnectedPlayer.PlayerId);
                }
                catch (Exception publishException)
                {
                    _logger.LogError(
                        publishException,
                        "Could not publish disconnected presence for player {PlayerId} in room {RoomCode}.",
                        disconnectedPlayer.PlayerId,
                        disconnectedPlayer.RoomCode);
                }
            }
        }
        finally
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
