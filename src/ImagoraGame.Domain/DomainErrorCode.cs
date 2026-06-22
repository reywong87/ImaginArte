namespace ImagoraGame.Domain;

public enum DomainErrorCode
{
    RoomIsFull = 0,
    PlayerAlreadyJoined = 1,
    NotEnoughPlayers = 2,
    RoomNotWaitingForPlayers = 3,
    RoomNotWaitingForStoryteller = 4,
    PlayerIsNotCurrentStoryteller = 5,
    StorytellerCardNotInHand = 6,
    ClueIsRequired = 7
}
