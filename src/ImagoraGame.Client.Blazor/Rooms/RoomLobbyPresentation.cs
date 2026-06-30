using ImagoraGame.Contracts.Rooms;

namespace ImagoraGame.Client.Blazor.Rooms;

public static class RoomLobbyPresentation
{
    public const int MinimumPlayersToStart = 3;

    public static bool ShowsStartButton(RoomStateResponse room)
    {
        return room.Phase == RoomPhase.WaitingForPlayers;
    }

    public static bool CanStart(RoomStateResponse room, bool isStarting)
    {
        return ShowsStartButton(room)
            && room.Players.Count >= MinimumPlayersToStart
            && !isStarting;
    }

    public static string? StorytellerName(RoomStateResponse room)
    {
        return room.Phase == RoomPhase.StorytellerChoosing
            ? room.CurrentStoryteller?.Name
            : null;
    }

    public static IEnumerable<RevealCardStateResponse> RevealedCardsForDisplay(RoomStateResponse room)
    {
        return room.RevealedCards.OrderBy(card => card.DisplayNumber);
    }
}
