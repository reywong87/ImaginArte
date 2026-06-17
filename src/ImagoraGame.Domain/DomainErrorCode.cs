namespace ImagoraGame.Domain;

public enum DomainErrorCode
{
    RoomIsFull = 0,
    PlayerAlreadyJoined = 1,
    NotEnoughPlayers = 2,
    RoomNotWaitingForPlayers = 3
}
