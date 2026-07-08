using ImaginArte.Application.Rooms;
using ImaginArte.Contracts.Rooms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

public sealed class RoomHub : Hub
{
    private readonly GetRoomStateHandler _getRoomState;
    private readonly GetPlayerPrivateStateHandler _getPlayerPrivateState;
    private readonly ILogger<RoomHub> _logger;

    public RoomHub(
        GetRoomStateHandler getRoomState,
        GetPlayerPrivateStateHandler getPlayerPrivateState,
        ILogger<RoomHub> logger)
    {
        _getRoomState = getRoomState;
        _getPlayerPrivateState = getPlayerPrivateState;
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
            var room = result.Room.ToHttpResponse();

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
            var state = result.ToHttpResponse();
            var normalizedRoomCode = state.Room.RoomCode;

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                RoomHubGroupNames.PublicRoom(normalizedRoomCode),
                cancellation.Token);
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                RoomHubGroupNames.PrivatePlayer(normalizedRoomCode, playerId),
                cancellation.Token);

            await Clients.Caller.SendAsync(
                RoomRealtimeEventNames.RoomUpdated,
                new RoomUpdatedEvent(state.Room),
                cancellation.Token);
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
}
