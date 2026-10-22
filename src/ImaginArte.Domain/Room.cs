namespace ImaginArte.Domain;

public sealed class Room
{
    public const int MinimumPlayers = 3;
    public const int MaximumPlayers = 8;
    public const int InitialHandSize = 6;
    public const int DefaultTargetScore = 25;
    private const int TemporaryDeckSize = 98;

    private readonly List<Player> _players = [];
    private readonly List<Card> _availableDeck = [];
    private readonly List<Card> _discardPile = [];
    private readonly List<GuessCardSubmission> _guessCardSubmissions = [];
    private readonly List<VotingCardOwnership> _votingCardOwnerships = [];
    private readonly List<Vote> _votes = [];
    private readonly List<PlayerScore> _scoreboard = [];
    private readonly IRandomNumberGenerator _randomNumberGenerator;
    private RoundScoreResult? _roundScoreResult;

    private Room(RoomCode code, int targetScore, IRandomNumberGenerator randomNumberGenerator)
    {
        if (targetScore <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetScore), targetScore, "Target score must be greater than zero.");
        }

        Code = code;
        TargetScore = targetScore;
        _randomNumberGenerator = randomNumberGenerator;
        Phase = GamePhase.WaitingForPlayers;
    }

    public RoomCode Code { get; }

    public GamePhase Phase { get; private set; }

    public int TargetScore { get; private set; }

    public PlayerId? CurrentStorytellerId { get; private set; }

    public CardId? StorytellerCardId { get; private set; }

    public string? Clue { get; private set; }

    public IReadOnlyCollection<Player> Players => _players.AsReadOnly();

    public IReadOnlyCollection<GuessCardSubmission> GuessCardSubmissions => _guessCardSubmissions.AsReadOnly();

    public IReadOnlyCollection<VotingCard> VotingCards => Phase is GamePhase.Voting or GamePhase.VoteReveal
        ? _votingCardOwnerships
            .Select((card, index) =>
            {
                var votes = _votes
                    .Where(vote => vote.VotingCardId == card.VotingCardId)
                    .Select(vote =>
                    {
                        var voter = _players.Single(player => player.Id == vote.VoterId);

                        return new VotingCardVote(voter.Id, voter.Name);
                    })
                    .ToArray();

                return new VotingCard(card.VotingCardId, card.CardId, index + 1, votes);
            })
            .ToArray()
        : Array.Empty<VotingCard>();

    public IReadOnlyCollection<VotingCardOwnership> VotingCardOwnerships => _votingCardOwnerships.AsReadOnly();

    public IReadOnlyCollection<PlayerRoundScore> RoundScores =>
        _roundScoreResult?.PlayerScores ?? Array.Empty<PlayerRoundScore>();

    public IReadOnlyCollection<PlayerScore> Scoreboard => _scoreboard.AsReadOnly();

    public IReadOnlyCollection<PlayerScore> Winners
    {
        get
        {
            if (Phase != GamePhase.GameComplete || _scoreboard.Count == 0)
            {
                return Array.Empty<PlayerScore>();
            }

            var highestScore = _scoreboard.Max(score => score.TotalPoints);

            if (highestScore < TargetScore)
            {
                return Array.Empty<PlayerScore>();
            }

            return _scoreboard
                .Where(score => score.TotalPoints == highestScore)
                .ToArray();
        }
    }

    public IReadOnlyCollection<RevealedVotingCard> RevealedVotingCards => Phase is GamePhase.Reveal
            or GamePhase.Scoring
            or GamePhase.RoundComplete
            or GamePhase.GameComplete
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

    public static Room Create(
        RoomCode code,
        int targetScore = DefaultTargetScore,
        IRandomNumberGenerator? randomNumberGenerator = null)
    {
        ArgumentNullException.ThrowIfNull(code);

        return new Room(code, targetScore, randomNumberGenerator ?? new SharedRandomNumberGenerator());
    }

    public RoomSnapshot ToSnapshot()
    {
        return new RoomSnapshot(
            Code,
            Phase,
            TargetScore,
            CurrentStorytellerId,
            StorytellerCardId,
            Clue,
            _players
                .Select(player => new PlayerSnapshot(player.Id, player.Name, player.AvatarId, player.Hand.ToArray()))
                .ToArray(),
            _availableDeck.ToArray(),
            _discardPile.ToArray(),
            _guessCardSubmissions.ToArray(),
            _votingCardOwnerships.ToArray(),
            _votes.ToArray(),
            _scoreboard.ToArray(),
            _roundScoreResult?.PlayerScores.ToArray() ?? Array.Empty<PlayerRoundScore>());
    }

    public static Room Restore(
        RoomSnapshot snapshot,
        IRandomNumberGenerator? randomNumberGenerator = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var room = new Room(
            snapshot.Code,
            snapshot.TargetScore,
            randomNumberGenerator ?? new SharedRandomNumberGenerator())
        {
            Phase = snapshot.Phase,
            CurrentStorytellerId = snapshot.CurrentStorytellerId,
            StorytellerCardId = snapshot.StorytellerCardId,
            Clue = snapshot.Clue
        };

        foreach (var playerSnapshot in snapshot.Players)
        {
            var player = new Player(playerSnapshot.Id, playerSnapshot.Name, playerSnapshot.AvatarId);

            if (room._players.Any(existingPlayer => existingPlayer.AvatarId == player.AvatarId))
            {
                throw new DomainRuleViolationException(
                    DomainErrorCode.AvatarAlreadyTaken,
                    "A room cannot assign the same avatar to more than one player.");
            }

            player.DealInitialHand(playerSnapshot.Hand);
            room._players.Add(player);
        }

        room._availableDeck.AddRange(snapshot.AvailableDeck);
        room._discardPile.AddRange(snapshot.DiscardPile);
        room._guessCardSubmissions.AddRange(snapshot.GuessCardSubmissions);
        room._votingCardOwnerships.AddRange(snapshot.VotingCardOwnerships);
        room._votes.AddRange(snapshot.Votes);
        room._scoreboard.AddRange(snapshot.Scoreboard);
        room._roundScoreResult = snapshot.RoundScores.Count > 0
            ? new RoundScoreResult(snapshot.RoundScores.ToArray())
            : null;

        return room;
    }

    public void SetTargetScore(int targetScore)
    {
        if (Phase != GamePhase.WaitingForPlayers)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.RoomNotWaitingForPlayers,
                "The target score can only be changed while the room is waiting for players.");
        }

        if (targetScore is not (15 or 25 or 35))
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.InvalidTargetScore,
                "The target score must be 15, 25, or 35.");
        }

        TargetScore = targetScore;
    }

    public Player Join(PlayerId playerId, string playerName, AvatarId avatarId)
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

        if (!avatarId.IsDefined)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.InvalidAvatar,
                "The avatar ID must be one of the supported avatars.");
        }

        var player = new Player(playerId, playerName, avatarId);

        if (_players.Any(existingPlayer => existingPlayer.Id == player.Id))
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.PlayerAlreadyJoined,
                "This player has already joined the room.");
        }

        if (_players.Any(existingPlayer => existingPlayer.AvatarId == player.AvatarId))
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.AvatarAlreadyTaken,
                "This avatar has already been chosen by another player in the room.");
        }

        _players.Add(player);
        _scoreboard.Add(new PlayerScore(player.Id, player.Name, 0));

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
            Phase = GamePhase.VoteReveal;
        }
    }

    public void RevealStorytellerCard()
    {
        if (Phase != GamePhase.VoteReveal)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.RoomNotReadyForStorytellerReveal,
                "The storyteller card can only be revealed after all votes have been locked.");
        }

        Phase = GamePhase.Reveal;
    }

    public RoundScoreResult ScoreRound()
    {
        if (Phase != GamePhase.Reveal)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.RoomNotReadyForScoring,
                "Round scores can only be calculated after votes are revealed.");
        }

        if (CurrentStorytellerId is not { } storytellerId)
        {
            throw new InvalidOperationException("Round scoring requires a current storyteller.");
        }

        var storytellerCard = _votingCardOwnerships.Single(card => card.IsStorytellerCard);
        var guessers = _players
            .Where(player => player.Id != storytellerId)
            .ToArray();
        var correctGuesserIds = _votes
            .Where(vote => vote.VotingCardId == storytellerCard.VotingCardId)
            .Select(vote => vote.VoterId)
            .ToHashSet();
        var scoresByPlayerId = _players.ToDictionary(player => player.Id, _ => 0);

        if (correctGuesserIds.Count == guessers.Length)
        {
            foreach (var guesser in guessers)
            {
                scoresByPlayerId[guesser.Id] += 2;
            }
        }
        else if (correctGuesserIds.Count == 0)
        {
            foreach (var guesser in guessers)
            {
                scoresByPlayerId[guesser.Id] += 2;
            }

            AwardGuesserCardVoteBonuses(scoresByPlayerId);
        }
        else
        {
            scoresByPlayerId[storytellerId] += 3;

            foreach (var correctGuesserId in correctGuesserIds)
            {
                scoresByPlayerId[correctGuesserId] += 3;
            }

            AwardGuesserCardVoteBonuses(scoresByPlayerId);
        }

        _roundScoreResult = new RoundScoreResult(_players
            .Select(player => new PlayerRoundScore(player.Id, player.Name, scoresByPlayerId[player.Id]))
            .ToArray());
        AddRoundScoresToScoreboard(_roundScoreResult.PlayerScores);
        Phase = _scoreboard.Any(score => score.TotalPoints >= TargetScore)
            ? GamePhase.GameComplete
            : GamePhase.Scoring;

        return _roundScoreResult;
    }

    public void CompleteRound()
    {
        if (Phase != GamePhase.Scoring)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.RoomNotInScoring,
                "A round can only be completed after scoring has been shown.");
        }

        Phase = GamePhase.RoundComplete;
    }

    public void AdvanceRound()
    {
        if (Phase != GamePhase.RoundComplete)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.RoomNotRoundComplete,
                "A new round can only start after the current round is complete.");
        }

        if (CurrentStorytellerId is not { } storytellerId)
        {
            throw new InvalidOperationException("Advancing a round requires a current storyteller.");
        }

        if (StorytellerCardId is not { } storytellerCardId)
        {
            throw new InvalidOperationException("Advancing a round requires a storyteller card.");
        }

        var currentStorytellerIndex = _players.FindIndex(player => player.Id == storytellerId);

        if (currentStorytellerIndex < 0)
        {
            throw new InvalidOperationException("The current storyteller must belong to the room.");
        }

        var usedCardsByPlayerId = _guessCardSubmissions.ToDictionary(
            submission => submission.PlayerId,
            submission => submission.CardId);
        usedCardsByPlayerId.Add(storytellerId, storytellerCardId);

        foreach (var player in _players)
        {
            var usedCardId = usedCardsByPlayerId[player.Id];
            var discardedCard = player.RemoveCard(usedCardId);
            _discardPile.Add(discardedCard);
        }

        RefillHands();

        var nextStorytellerIndex = (currentStorytellerIndex + 1) % _players.Count;
        CurrentStorytellerId = _players[nextStorytellerIndex].Id;
        StorytellerCardId = null;
        Clue = null;
        _guessCardSubmissions.Clear();
        _votingCardOwnerships.Clear();
        _votes.Clear();
        _roundScoreResult = null;
        Phase = GamePhase.StorytellerChoosing;
    }

    private void PrepareVotingCards()
    {
        if (CurrentStorytellerId is not { } storytellerId || StorytellerCardId is not { } storytellerCardId)
        {
            throw new InvalidOperationException("Voting cards require a storyteller and storyteller card.");
        }

        _votingCardOwnerships.Clear();
        _votes.Clear();
        _roundScoreResult = null;

        var guesserSubmissionsByPlayerId = _guessCardSubmissions.ToDictionary(
            submission => submission.PlayerId,
            submission => submission);
        var votingCardCandidates = new List<VotingCardCandidate>
        {
            new(storytellerCardId, storytellerId, IsStorytellerCard: true)
        };

        foreach (var guesser in _players.Where(player => player.Id != storytellerId))
        {
            var submission = guesserSubmissionsByPlayerId[guesser.Id];

            votingCardCandidates.Add(new VotingCardCandidate(
                submission.CardId,
                guesser.Id,
                IsStorytellerCard: false));
        }

        Shuffle(votingCardCandidates);

        foreach (var candidate in votingCardCandidates)
        {
            AddVotingCard(candidate.CardId, candidate.OwnerPlayerId, candidate.IsStorytellerCard);
        }
    }

    private void Shuffle<T>(IList<T> items)
    {
        for (var currentIndex = items.Count - 1; currentIndex > 0; currentIndex--)
        {
            var swapIndex = _randomNumberGenerator.Next(currentIndex + 1);
            (items[currentIndex], items[swapIndex]) = (items[swapIndex], items[currentIndex]);
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

    private void AwardGuesserCardVoteBonuses(Dictionary<PlayerId, int> scoresByPlayerId)
    {
        var guesserVotingCardsById = _votingCardOwnerships
            .Where(card => !card.IsStorytellerCard)
            .ToDictionary(card => card.VotingCardId, card => card);

        foreach (var vote in _votes)
        {
            if (guesserVotingCardsById.TryGetValue(vote.VotingCardId, out var votedCard))
            {
                scoresByPlayerId[votedCard.OwnerPlayerId] += 1;
            }
        }
    }

    private void AddRoundScoresToScoreboard(IReadOnlyCollection<PlayerRoundScore> roundScores)
    {
        var roundScoresByPlayerId = roundScores.ToDictionary(score => score.PlayerId);

        for (var index = 0; index < _scoreboard.Count; index++)
        {
            var currentScore = _scoreboard[index];
            var roundScore = roundScoresByPlayerId[currentScore.PlayerId];

            _scoreboard[index] = currentScore with
            {
                TotalPoints = currentScore.TotalPoints + roundScore.PointsAwarded
            };
        }
    }

    private void DealInitialHands()
    {
        _availableDeck.Clear();
        _availableDeck.AddRange(CreateTemporaryDeck());
        _discardPile.Clear();

        foreach (var player in _players)
        {
            var hand = DrawCards(InitialHandSize);

            player.DealInitialHand(hand);
        }
    }

    private void RefillHands()
    {
        foreach (var player in _players)
        {
            var cardsNeeded = InitialHandSize - player.Hand.Count;

            foreach (var card in DrawCards(cardsNeeded))
            {
                player.AddCard(card);
            }
        }
    }

    private IReadOnlyCollection<Card> DrawCards(int count)
    {
        if (count < 0)
        {
            throw new InvalidOperationException("A player hand cannot exceed the initial hand size.");
        }

        if (_availableDeck.Count < count && _availableDeck.Count + _discardPile.Count < count)
        {
            throw new InvalidOperationException("The available deck does not contain enough cards.");
        }

        if (_availableDeck.Count < count)
        {
            var recycledCards = _discardPile.ToArray();
            Shuffle(recycledCards);
            _availableDeck.AddRange(recycledCards);
            _discardPile.Clear();
        }

        var cards = _availableDeck.Take(count).ToArray();
        _availableDeck.RemoveRange(0, count);

        return cards;
    }

    private static IReadOnlyList<Card> CreateTemporaryDeck()
    {
        return Enumerable
            .Range(1, TemporaryDeckSize)
            .Select(cardNumber => new Card(new CardId($"TEMP-{cardNumber:000}")))
            .ToArray();
    }

    private sealed record VotingCardCandidate(
        CardId CardId,
        PlayerId OwnerPlayerId,
        bool IsStorytellerCard);
}
