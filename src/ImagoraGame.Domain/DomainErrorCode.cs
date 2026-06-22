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
    ClueIsRequired = 7,
    RoomNotWaitingForGuessers = 8,
    PlayerNotInRoom = 9,
    PlayerIsCurrentStoryteller = 10,
    GuessCardNotInHand = 11,
    GuessCardAlreadySubmitted = 12
}
