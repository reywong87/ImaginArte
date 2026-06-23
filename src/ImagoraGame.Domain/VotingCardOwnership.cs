namespace ImagoraGame.Domain;

public sealed record VotingCardOwnership(
    VotingCardId VotingCardId,
    CardId CardId,
    PlayerId OwnerPlayerId,
    bool IsStorytellerCard);
