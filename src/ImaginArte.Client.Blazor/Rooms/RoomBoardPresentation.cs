using ImaginArte.Contracts.Rooms;

namespace ImaginArte.Client.Blazor.Rooms;

public sealed record BoardTrackPosition(int Position);

public sealed record BoardPlayerToken(
    Guid PlayerId,
    string PlayerName,
    string Label,
    int TotalPoints,
    int VisualPosition,
    int StackIndex);

public sealed record RoomBoardPresentation(
    int MaxVisualPoints,
    IReadOnlyList<BoardTrackPosition> Track,
    IReadOnlyList<BoardPlayerToken> Tokens)
{
    public const int DefaultMaxVisualPoints = 25;

    public static RoomBoardPresentation FromRoom(
        RoomStateResponse room,
        int maxVisualPoints = DefaultMaxVisualPoints)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxVisualPoints);

        var track = Enumerable.Range(0, maxVisualPoints + 1)
            .Select(position => new BoardTrackPosition(position))
            .ToArray();

        var orderedTokens = room.Scoreboard
            .Select(score => new
            {
                score.PlayerId,
                score.PlayerName,
                score.TotalPoints,
                VisualPosition = Math.Clamp(score.TotalPoints, 0, maxVisualPoints)
            })
            .OrderBy(score => score.VisualPosition)
            .ThenBy(score => score.PlayerName, StringComparer.Ordinal)
            .ThenBy(score => score.PlayerId)
            .ToArray();

        var tokens = orderedTokens
            .GroupBy(score => score.VisualPosition)
            .SelectMany(group => group.Select((score, index) => new BoardPlayerToken(
                score.PlayerId,
                score.PlayerName,
                CreateLabel(score.PlayerName),
                score.TotalPoints,
                score.VisualPosition,
                index)))
            .ToArray();

        return new RoomBoardPresentation(maxVisualPoints, track, tokens);
    }

    private static string CreateLabel(string playerName)
    {
        var trimmedName = playerName.Trim();

        return trimmedName.Length == 0
            ? "?"
            : trimmedName[..1].ToUpperInvariant();
    }
}
