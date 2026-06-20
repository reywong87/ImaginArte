namespace ImagoraGame.Domain;

public sealed class Room
{
    public const int MinimumPlayers = 3;
    public const int MaximumPlayers = 8;
    public const int InitialHandSize = 6;

    private readonly List<Player> _players = [];

    private Room(RoomCode code)
    {
        Code = code;
        Phase = GamePhase.WaitingForPlayers;
    }

    public RoomCode Code { get; }

    public GamePhase Phase { get; private set; }

    public PlayerId? CurrentStorytellerId { get; private set; }

    public IReadOnlyCollection<Player> Players => _players.AsReadOnly();

    public IReadOnlyCollection<Card> GetHandFor(PlayerId playerId)
    {
        var player = _players.SingleOrDefault(existingPlayer => existingPlayer.Id == playerId);

        if (player is null)
        {
            throw new ArgumentException("Player is not in this room.", nameof(playerId));
        }

        return player.Hand;
    }

    public static Room Create(RoomCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return new Room(code);
    }

    public Player Join(PlayerId playerId, string playerName)
    {
        if (Phase != GamePhase.WaitingForPlayers)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.RoomNotWaitingForPlayers,
                "Players can only join while the room is waiting for players.");
        }

        if (_players.Count >= MaximumPlayers)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.RoomIsFull,
                $"A room cannot have more than {MaximumPlayers} players.");
        }

        var player = new Player(playerId, playerName);

        if (_players.Any(existingPlayer => existingPlayer.Id == player.Id))
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.PlayerAlreadyJoined,
                "This player has already joined the room.");
        }

        _players.Add(player);

        return player;
    }

    public void StartGame()
    {
        if (Phase != GamePhase.WaitingForPlayers)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.RoomNotWaitingForPlayers,
                "Only rooms waiting for players can start a game.");
        }

        if (_players.Count < MinimumPlayers)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.NotEnoughPlayers,
                $"A game needs at least {MinimumPlayers} players to start.");
        }

        CurrentStorytellerId = _players[0].Id;
        DealInitialHands();
        Phase = GamePhase.StorytellerChoosing;
    }

    private void DealInitialHands()
    {
        var deck = CreateTemporaryDeck();

        for (var playerIndex = 0; playerIndex < _players.Count; playerIndex++)
        {
            var hand = deck
                .Skip(playerIndex * InitialHandSize)
                .Take(InitialHandSize);

            _players[playerIndex].DealInitialHand(hand);
        }
    }

    private static IReadOnlyList<Card> CreateTemporaryDeck()
    {
        return Enumerable
            .Range(1, MaximumPlayers * InitialHandSize)
            .Select(cardNumber => new Card(new CardId($"TEMP-{cardNumber:000}")))
            .ToArray();
    }
}
