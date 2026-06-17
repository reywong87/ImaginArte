namespace ImagoraGame.Domain;

public enum GamePhase
{
    WaitingForPlayers = 0,
    StorytellerChoosing = 1,
    GuessersSubmitting = 2,
    Voting = 3,
    Reveal = 4,
    Scoring = 5,
    RoundComplete = 6,
    GameComplete = 7
}
