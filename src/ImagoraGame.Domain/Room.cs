namespace ImagoraGame.Domain;

public sealed class Room
{
    public const int MinimumPlayers = 3;
    public const int MaximumPlayers = 8;
    public const int InitialHandSize = 6;

    private readonly List<Player> _players = [];
    private readonly List<GuessCardSubmission> _guessCardSubmissions = [];
    private readonly List<VotingCardOwnership> _votingCardOwnerships = [];
    private readonly List<Vote> _votes = [];

    private Room(RoomCode code)
    {
        Code = code;
        Phase = GamePhase.WaitingForPlayers;
    }

    public RoomCode Code { get; }

    public GamePhase Phase { get; private set; }

    public PlayerId? CurrentStorytellerId { get; private set; }

    public CardId? StorytellerCardId { get; private set; }

    public string? Clue { get; private set; }

    public IReadOnlyCollection<Player> Players => _players.AsReadOnly();

    public IReadOnlyCollection<GuessCardSubmission> GuessCardSubmissions => _guessCardSubmissions.AsReadOnly();

    public IReadOnlyCollection<VotingCard> VotingCards => Phase == GamePhase.Voting
        ? _votingCardOwnerships
            .Select((card, index) => new VotingCard(card.VotingCardId, card.CardId, index + 1))
            .ToArray()
        : Array.Empty<VotingCard>();

    public IReadOnlyCollection<VotingCardOwnership> VotingCardOwnerships => _votingCardOwnerships.AsReadOnly();

    public IReadOnlyCollection<RevealedVotingCard> RevealedVotingCards => Phase == GamePhase.Reveal
        ? _votingCardOwnerships
            .Select((card, index) =>
            {
                var owner = _players.Single(player => player.Id == card.OwnerPlayerId);
                var votes = _votes
                    .Where(vote => vote.VotingCardId == card.VotingCardId)
                    .Select(vote =>
                    {
                        var voter = _players.Single(player => player.Id == vote.VoterId);

                        return new RevealedVote(voter.Id, voter.Name);
                    })
                    .ToArray();

                return new RevealedVotingCard(
                    card.VotingCardId,
                    card.CardId,
                    index + 1,
                    owner.Id,
                    owner.Name,
                    card.IsStorytellerCard,
                    votes);
            })
            .ToArray()
        : Array.Empty<RevealedVotingCard>();

    public int SubmissionCount => _guessCardSubmissions.Count;

    public int ExpectedSubmissionCount => CurrentStorytellerId is null
        ? 0
        : _players.Count(player => player.Id != CurrentStorytellerId);

    public int VoteCount => _votes.Count;

    public int ExpectedVoteCount => CurrentStorytellerId is null
        ? 0
        : _players.Count(player => player.Id != CurrentStorytellerId);

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

    public void ChooseStorytellerCard(PlayerId playerId, CardId cardId, string clue)
    {
        if (Phase != GamePhase.StorytellerChoosing)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.RoomNotWaitingForStoryteller,
                "The storyteller can only choose a card while the room is waiting for the storyteller.");
        }

        if (CurrentStorytellerId != playerId)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.PlayerIsNotCurrentStoryteller,
                "Only the current storyteller can choose the storyteller card.");
        }

        if (string.IsNullOrWhiteSpace(clue))
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.ClueIsRequired,
                "The storyteller clue is required.");
        }

        var storyteller = _players.Single(player => player.Id == playerId);

        if (!storyteller.Hand.Any(card => card.Id == cardId))
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.StorytellerCardNotInHand,
                "The storyteller card must be in the storyteller's hand.");
        }

        StorytellerCardId = cardId;
        Clue = clue.Trim();
        Phase = GamePhase.GuessersSubmitting;
    }

    public void SubmitGuessCard(PlayerId playerId, CardId cardId)
    {
        if (Phase != GamePhase.GuessersSubmitting)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.RoomNotWaitingForGuessers,
                "Guessers can only submit cards while the room is waiting for guesser submissions.");
        }

        var player = _players.SingleOrDefault(existingPlayer => existingPlayer.Id == playerId);

        if (player is null)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.PlayerNotInRoom,
                "The player must belong to the room to submit a guess card.");
        }

        if (CurrentStorytellerId == playerId)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.PlayerIsCurrentStoryteller,
                "The current storyteller cannot submit a guess card.");
        }

        if (_guessCardSubmissions.Any(submission => submission.PlayerId == playerId))
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.GuessCardAlreadySubmitted,
                "This guesser has already submitted a guess card.");
        }

        if (!player.Hand.Any(card => card.Id == cardId))
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.GuessCardNotInHand,
                "The guess card must be in the guesser's hand.");
        }

        _guessCardSubmissions.Add(new GuessCardSubmission(playerId, cardId));

        if (SubmissionCount == ExpectedSubmissionCount)
        {
            PrepareVotingCards();
            Phase = GamePhase.Voting;
        }
    }

    public void CastVote(PlayerId voterId, VotingCardId votingCardId)
    {
        if (Phase != GamePhase.Voting)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.RoomNotWaitingForVotes,
                "Votes can only be cast while the room is waiting for votes.");
        }

        if (!_players.Any(player => player.Id == voterId))
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.PlayerNotInRoom,
                "The player must belong to the room to cast a vote.");
        }

        if (CurrentStorytellerId == voterId)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.PlayerIsCurrentStoryteller,
                "The current storyteller cannot vote.");
        }

        if (_votes.Any(vote => vote.VoterId == voterId))
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.PlayerAlreadyVoted,
                "This guesser has already voted.");
        }

        var votingCard = _votingCardOwnerships.SingleOrDefault(card => card.VotingCardId == votingCardId);

        if (votingCard is null)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.VotingCardNotFound,
                "The selected voting card must be one of the votable options.");
        }

        if (votingCard.OwnerPlayerId == voterId && !votingCard.IsStorytellerCard)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.PlayerCannotVoteForOwnCard,
                "A guesser cannot vote for their own submitted card.");
        }

        _votes.Add(new Vote(voterId, votingCardId));

        if (VoteCount == ExpectedVoteCount)
        {
            Phase = GamePhase.Reveal;
        }
    }

    private void PrepareVotingCards()
    {
        if (CurrentStorytellerId is not { } storytellerId || StorytellerCardId is not { } storytellerCardId)
        {
            throw new InvalidOperationException("Voting cards require a storyteller and storyteller card.");
        }

        _votingCardOwnerships.Clear();
        _votes.Clear();

        AddVotingCard(storytellerCardId, storytellerId, isStorytellerCard: true);

        var guesserSubmissionsByPlayerId = _guessCardSubmissions.ToDictionary(
            submission => submission.PlayerId,
            submission => submission);

        foreach (var guesser in _players.Where(player => player.Id != storytellerId))
        {
            var submission = guesserSubmissionsByPlayerId[guesser.Id];

            AddVotingCard(submission.CardId, guesser.Id, isStorytellerCard: false);
        }
    }

    private void AddVotingCard(CardId cardId, PlayerId ownerPlayerId, bool isStorytellerCard)
    {
        var displayNumber = _votingCardOwnerships.Count + 1;
        var votingCardId = new VotingCardId($"VOTE-{displayNumber:000}");

        _votingCardOwnerships.Add(new VotingCardOwnership(
            votingCardId,
            cardId,
            ownerPlayerId,
            isStorytellerCard));
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
