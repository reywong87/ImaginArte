namespace ImagoraGame.Domain;

public sealed record VotingCard(
    VotingCardId VotingCardId,
    CardId CardId,
    int DisplayNumber,
    IReadOnlyCollection<VotingCardVote> Votes);
