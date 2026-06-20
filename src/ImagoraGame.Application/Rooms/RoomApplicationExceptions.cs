namespace ImagoraGame.Application.Rooms;

public sealed class RoomAlreadyExistsException : InvalidOperationException
{
    public RoomAlreadyExistsException(string roomCode)
        : base($"Room '{roomCode}' already exists.")
    {
        RoomCode = roomCode;
    }

    public string RoomCode { get; }
}

public sealed class RoomNotFoundException : InvalidOperationException
{
    public RoomNotFoundException(string roomCode)
        : base($"Room '{roomCode}' was not found.")
    {
        RoomCode = roomCode;
    }

    public string RoomCode { get; }
}

public sealed class PlayerNotFoundInRoomException : InvalidOperationException
{
    public PlayerNotFoundInRoomException(string roomCode, Guid playerId)
        : base($"Player '{playerId}' was not found in room '{roomCode}'.")
    {
        RoomCode = roomCode;
        PlayerId = playerId;
    }

    public string RoomCode { get; }

    public Guid PlayerId { get; }
}
