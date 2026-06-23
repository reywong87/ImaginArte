namespace ImagoraGame.Domain;

public sealed record RevealedVotingCard(
    VotingCardId VotingCardId,
    CardId CardId,
    int DisplayNumber,
    PlayerId OwnerPlayerId,
    string OwnerName,
    bool IsStorytellerCard,
    IReadOnlyCollection<RevealedVote> Votes);
