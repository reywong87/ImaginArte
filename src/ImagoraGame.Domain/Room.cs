namespace ImagoraGame.Domain;

public sealed class Room
{
    public const int MinimumPlayers = 3;
    public const int MaximumPlayers = 8;
    public const int InitialHandSize = 6;
    public const int DefaultTargetScore = 25;

    private readonly List<Player> _players = [];
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

    public int TargetScore { get; }

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
            Phase = GamePhase.Reveal;
        }
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

        var currentStorytellerIndex = _players.FindIndex(player => player.Id == storytellerId);

        if (currentStorytellerIndex < 0)
        {
            throw new InvalidOperationException("The current storyteller must belong to the room.");
        }

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

    private sealed record VotingCardCandidate(
        CardId CardId,
        PlayerId OwnerPlayerId,
        bool IsStorytellerCard);
}
