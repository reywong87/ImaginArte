namespace ImaginArte.Domain;

public sealed record VotingCardVote(
    PlayerId VoterPlayerId,
    string VoterName);
