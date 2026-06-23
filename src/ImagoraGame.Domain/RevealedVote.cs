namespace ImagoraGame.Domain;

public sealed record RevealedVote(
    PlayerId VoterPlayerId,
    string VoterName);
