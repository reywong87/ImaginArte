using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ImagoraGame.Application.Rooms;
using ImagoraGame.Contracts.Rooms;
using ImagoraGame.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ImagoraGame.Server.Tests;

public sealed class RoomEndpointTests
{
    [Fact]
    public async Task PostRooms_CreatesRoomAndNormalizesRoomCode()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.PostAsJsonAsync("/rooms", new
        {
            RoomCode = " family-room "
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/rooms/FAMILY-ROOM", response.Headers.Location?.OriginalString);

        var room = await ReadRequiredJsonAsync<RoomState>(response);
        Assert.Equal("FAMILY-ROOM", room.RoomCode);
        Assert.Equal(GamePhase.WaitingForPlayers, room.Phase);
        Assert.Empty(room.Players);
    }

    [Fact]
    public async Task PostRooms_WithDuplicateRoomCode_ReturnsConflict()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        await CreateRoomAsync(client, "ABC123");

        var response = await client.PostAsJsonAsync("/rooms", new
        {
            RoomCode = "abc123"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
    }

    [Fact]
    public async Task PostPlayers_JoinsPlayerAndReturnsUpdatedRoomState()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var playerId = Guid.NewGuid();

        await CreateRoomAsync(client, "ABC123");

        var response = await client.PostAsJsonAsync("/rooms/abc123/players", new
        {
            PlayerId = playerId,
            PlayerName = " Alice "
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var room = await ReadRequiredJsonAsync<RoomState>(response);
        Assert.Equal("ABC123", room.RoomCode);
        Assert.Collection(
            room.Players,
            player =>
            {
                Assert.Equal(playerId, player.PlayerId);
                Assert.Equal("Alice", player.Name);
            });
    }

    [Fact]
    public async Task PostStart_WithFewerThanThreePlayers_ReturnsBadRequest()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        await CreateRoomAsync(client, "ABC123");
        await JoinPlayerAsync(client, "ABC123", Guid.NewGuid(), "Alice");
        await JoinPlayerAsync(client, "ABC123", Guid.NewGuid(), "Bob");

        var response = await client.PostAsync("/rooms/ABC123/start", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
    }

    [Fact]
    public async Task PostStart_WithThreePlayers_MovesRoomToStorytellerChoosing()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var aliceId = Guid.NewGuid();

        await CreateRoomAsync(client, "ABC123");
        await JoinPlayerAsync(client, "ABC123", aliceId, "Alice");
        await JoinPlayerAsync(client, "ABC123", Guid.NewGuid(), "Bob");
        await JoinPlayerAsync(client, "ABC123", Guid.NewGuid(), "Carla");

        var response = await client.PostAsync("/rooms/ABC123/start", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var room = await ReadRequiredJsonAsync<RoomState>(response);
        Assert.Equal(GamePhase.StorytellerChoosing, room.Phase);
        Assert.Equal(aliceId, room.CurrentStoryteller?.PlayerId);
        Assert.Equal("Alice", room.CurrentStoryteller?.Name);
    }

    [Fact]
    public async Task PostStorytellerCard_WithValidRequest_ReturnsUpdatedPublicRoomState()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var aliceId = Guid.NewGuid();

        await CreateStartedRoomAsync(client, "ABC123", aliceId);
        var privateState = await GetPlayerPrivateStateAsync(client, "ABC123", aliceId);
        var chosenCardId = privateState.Player.Hand.First().CardId;

        var response = await client.PostAsJsonAsync(
            "/rooms/abc123/storyteller-card",
            new ChooseStorytellerCardRequest(
                aliceId,
                chosenCardId,
                "  A strange doorway  "));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var room = ReadRequiredJson<RoomStateResponse>(responseBody);
        Assert.Equal(RoomPhase.GuessersSubmitting, room.Phase);
        Assert.Equal("A strange doorway", room.Clue);
        Assert.Equal(aliceId, room.CurrentStoryteller?.PlayerId);
        Assert.Equal("Alice", room.CurrentStoryteller?.Name);
        Assert.DoesNotContain("\"hand\"", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(chosenCardId, responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostStorytellerCard_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/rooms/missing/storyteller-card",
            new ChooseStorytellerCardRequest(
                Guid.NewGuid(),
                "TEMP-001",
                "A strange doorway"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Fact]
    public async Task PostStorytellerCard_WhenPlayerIsNotStoryteller_ReturnsBadRequest()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();

        await CreateStartedRoomAsync(client, "ABC123", aliceId, bobId);
        var bobPrivateState = await GetPlayerPrivateStateAsync(client, "ABC123", bobId);

        var response = await client.PostAsJsonAsync(
            "/rooms/ABC123/storyteller-card",
            new ChooseStorytellerCardRequest(
                bobId,
                bobPrivateState.Player.Hand.First().CardId,
                "A strange doorway"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
    }

    [Fact]
    public async Task PostStorytellerCard_WithEmptyClue_ReturnsBadRequest()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var aliceId = Guid.NewGuid();

        await CreateStartedRoomAsync(client, "ABC123", aliceId);
        var privateState = await GetPlayerPrivateStateAsync(client, "ABC123", aliceId);

        var response = await client.PostAsJsonAsync(
            "/rooms/ABC123/storyteller-card",
            new ChooseStorytellerCardRequest(
                aliceId,
                privateState.Player.Hand.First().CardId,
                string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
    }

    [Fact]
    public async Task PostGuessCards_WithFirstValidSubmission_ReturnsPublicSubmittingState()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var room = await PrepareGuessSubmissionRoomAsync(client, "ABC123");

        var response = await client.PostAsJsonAsync(
            "/rooms/abc123/guess-cards",
            new SubmitGuessCardRequest(room.BobId, room.BobCardIds[0]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var state = ReadRequiredJson<RoomStateResponse>(responseBody);
        Assert.Equal(RoomPhase.GuessersSubmitting, state.Phase);
        Assert.Equal(1, state.SubmissionCount);
        Assert.Equal(2, state.ExpectedSubmissionCount);
        Assert.Empty(state.VotingCards);
        Assert.DoesNotContain("\"hand\"", responseBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostGuessCards_WithLastSubmission_MovesToVotingWithoutRevealingCardOwnership()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var room = await PrepareGuessSubmissionRoomAsync(client, "ABC123");

        var firstResponse = await client.PostAsJsonAsync(
            "/rooms/ABC123/guess-cards",
            new SubmitGuessCardRequest(room.BobId, room.BobCardIds[0]));
        firstResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            "/rooms/ABC123/guess-cards",
            new SubmitGuessCardRequest(room.CarlaId, room.CarlaCardIds[0]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var state = ReadRequiredJson<RoomStateResponse>(responseBody);
        Assert.Equal(RoomPhase.Voting, state.Phase);
        Assert.Equal(state.ExpectedSubmissionCount, state.SubmissionCount);
        Assert.Equal(
            [room.StorytellerCardId, room.BobCardIds[0], room.CarlaCardIds[0]],
            state.VotingCards.Select(card => card.CardId).Order().ToArray());
        Assert.Equal(3, state.VotingCards.Select(card => card.CardId).Distinct().Count());
        Assert.Equal([1, 2, 3], state.VotingCards.Select(card => card.DisplayNumber).Order().ToArray());
        Assert.Equal(
            ["VOTE-001", "VOTE-002", "VOTE-003"],
            state.VotingCards.Select(card => card.VotingCardId).Order().ToArray());
        Assert.All(
            state.VotingCards,
            card => Assert.Equal($"VOTE-{card.DisplayNumber:000}", card.VotingCardId));
        Assert.DoesNotContain("\"hand\"", responseBody, StringComparison.OrdinalIgnoreCase);

        using var json = JsonDocument.Parse(responseBody);
        foreach (var votingCard in json.RootElement.GetProperty("votingCards").EnumerateArray())
        {
            Assert.False(votingCard.TryGetProperty("ownerPlayerId", out _));
            Assert.False(votingCard.TryGetProperty("ownerName", out _));
            Assert.False(votingCard.TryGetProperty("isStorytellerCard", out _));
        }
    }

    [Fact]
    public async Task PostGuessCards_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/rooms/missing/guess-cards",
            new SubmitGuessCardRequest(Guid.NewGuid(), "TEMP-001"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Fact]
    public async Task PostGuessCards_WhenStorytellerSubmits_ReturnsBadRequest()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var room = await PrepareGuessSubmissionRoomAsync(client, "ABC123");

        var response = await client.PostAsJsonAsync(
            "/rooms/ABC123/guess-cards",
            new SubmitGuessCardRequest(room.AliceId, room.StorytellerCardId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
    }

    [Fact]
    public async Task PostGuessCards_WhenGuesserSubmitsTwice_ReturnsBadRequest()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var room = await PrepareGuessSubmissionRoomAsync(client, "ABC123");

        var firstResponse = await client.PostAsJsonAsync(
            "/rooms/ABC123/guess-cards",
            new SubmitGuessCardRequest(room.BobId, room.BobCardIds[0]));
        firstResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            "/rooms/ABC123/guess-cards",
            new SubmitGuessCardRequest(room.BobId, room.BobCardIds[1]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
    }

    [Fact]
    public async Task PostGuessCards_WhenCardIsNotInPlayerHand_ReturnsBadRequest()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var room = await PrepareGuessSubmissionRoomAsync(client, "ABC123");

        var response = await client.PostAsJsonAsync(
            "/rooms/ABC123/guess-cards",
            new SubmitGuessCardRequest(room.BobId, room.CarlaCardIds[0]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
    }

    [Fact]
    public async Task PostVotes_WithFirstValidVote_ReturnsPrivateVotingState()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var room = await PrepareVotingRoomAsync(client, "ABC123");
        var storytellerVotingCardId = room.VotingState.VotingCards
            .Single(card => card.CardId == room.StorytellerCardId)
            .VotingCardId;

        var response = await client.PostAsJsonAsync(
            "/rooms/abc123/votes",
            new CastVoteRequest(room.BobId, storytellerVotingCardId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var state = ReadRequiredJson<RoomStateResponse>(responseBody);
        Assert.Equal(RoomPhase.Voting, state.Phase);
        Assert.Equal(1, state.VoteCount);
        Assert.Equal(2, state.ExpectedVoteCount);
        Assert.Empty(state.RevealedCards);
        Assert.DoesNotContain("voterPlayerId", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("voterName", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ownerPlayerId", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ownerName", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("isStorytellerCard", responseBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostVotes_WithLastVote_MovesToRevealAndRevealsVoteDetails()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var room = await PrepareVotingRoomAsync(client, "ABC123");
        var storytellerVotingCardId = room.VotingState.VotingCards
            .Single(card => card.CardId == room.StorytellerCardId)
            .VotingCardId;
        var bobVotingCardId = room.VotingState.VotingCards
            .Single(card => card.CardId == room.BobCardId)
            .VotingCardId;

        var firstResponse = await client.PostAsJsonAsync(
            "/rooms/ABC123/votes",
            new CastVoteRequest(room.BobId, storytellerVotingCardId));
        firstResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            "/rooms/ABC123/votes",
            new CastVoteRequest(room.CarlaId, bobVotingCardId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var state = await ReadRequiredJsonAsync<RoomStateResponse>(response);
        Assert.Equal(RoomPhase.Reveal, state.Phase);
        Assert.Equal(state.ExpectedVoteCount, state.VoteCount);
        Assert.Empty(state.VotingCards);
        Assert.Equal(room.VotingState.VotingCards.Count, state.RevealedCards.Count);
        Assert.Equal(
            room.VotingState.VotingCards.Select(
                card => (card.VotingCardId, card.CardId, card.DisplayNumber)),
            state.RevealedCards.Select(
                card => (card.VotingCardId, card.CardId, card.DisplayNumber)));

        var storytellerCard = state.RevealedCards.Single(
            card => card.CardId == room.StorytellerCardId);
        Assert.Equal(room.AliceId, storytellerCard.OwnerPlayerId);
        Assert.Equal("Alice", storytellerCard.OwnerName);
        Assert.True(storytellerCard.IsStorytellerCard);
        Assert.Collection(
            storytellerCard.Votes,
            vote =>
            {
                Assert.Equal(room.BobId, vote.VoterPlayerId);
                Assert.Equal("Bob", vote.VoterName);
            });

        var bobCard = state.RevealedCards.Single(card => card.CardId == room.BobCardId);
        Assert.Equal(room.BobId, bobCard.OwnerPlayerId);
        Assert.Equal("Bob", bobCard.OwnerName);
        Assert.False(bobCard.IsStorytellerCard);
        Assert.Collection(
            bobCard.Votes,
            vote =>
            {
                Assert.Equal(room.CarlaId, vote.VoterPlayerId);
                Assert.Equal("Carla", vote.VoterName);
            });

        var carlaCard = state.RevealedCards.Single(card => card.CardId == room.CarlaCardId);
        Assert.Equal(room.CarlaId, carlaCard.OwnerPlayerId);
        Assert.Equal("Carla", carlaCard.OwnerName);
        Assert.False(carlaCard.IsStorytellerCard);
        Assert.Empty(carlaCard.Votes);
    }

    [Fact]
    public async Task PostVotes_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/rooms/missing/votes",
            new CastVoteRequest(Guid.NewGuid(), "VOTE-001"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Fact]
    public async Task PostVotes_WhenStorytellerVotes_ReturnsBadRequest()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var room = await PrepareVotingRoomAsync(client, "ABC123");

        var response = await client.PostAsJsonAsync(
            "/rooms/ABC123/votes",
            new CastVoteRequest(
                room.AliceId,
                room.VotingState.VotingCards.First().VotingCardId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostVotes_WhenPlayerVotesTwice_ReturnsBadRequest()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var room = await PrepareVotingRoomAsync(client, "ABC123");
        var storytellerVotingCardId = room.VotingState.VotingCards
            .Single(card => card.CardId == room.StorytellerCardId)
            .VotingCardId;
        var carlaVotingCardId = room.VotingState.VotingCards
            .Single(card => card.CardId == room.CarlaCardId)
            .VotingCardId;

        var firstResponse = await client.PostAsJsonAsync(
            "/rooms/ABC123/votes",
            new CastVoteRequest(room.BobId, storytellerVotingCardId));
        firstResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            "/rooms/ABC123/votes",
            new CastVoteRequest(room.BobId, carlaVotingCardId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostVotes_WhenPlayerVotesForOwnCard_ReturnsBadRequest()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var room = await PrepareVotingRoomAsync(client, "ABC123");
        var bobVotingCardId = room.VotingState.VotingCards
            .Single(card => card.CardId == room.BobCardId)
            .VotingCardId;

        var response = await client.PostAsJsonAsync(
            "/rooms/ABC123/votes",
            new CastVoteRequest(room.BobId, bobVotingCardId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostVotes_WhenVotingCardDoesNotExist_ReturnsBadRequest()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var room = await PrepareVotingRoomAsync(client, "ABC123");

        var response = await client.PostAsJsonAsync(
            "/rooms/ABC123/votes",
            new CastVoteRequest(room.BobId, "VOTE-999"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostVotes_WhenRoomIsNotInVoting_ReturnsBadRequest()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var room = await PrepareGuessSubmissionRoomAsync(client, "ABC123");

        var response = await client.PostAsJsonAsync(
            "/rooms/ABC123/votes",
            new CastVoteRequest(room.BobId, "VOTE-001"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostScore_WithRevealRoom_ReturnsScoringStateAndPreservesRevealDetails()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var room = await PrepareRevealRoomAsync(client, "ABC123");

        var response = await client.PostAsync("/rooms/abc123/score", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var state = await ReadRequiredJsonAsync<RoomStateResponse>(response);
        Assert.Equal(RoomPhase.Scoring, state.Phase);
        Assert.Empty(state.Winners);
        AssertPlayerRoundScore(state.RoundScores, room.AliceId, "Alice", 3);
        AssertPlayerRoundScore(state.RoundScores, room.BobId, "Bob", 4);
        AssertPlayerRoundScore(state.RoundScores, room.CarlaId, "Carla", 0);
        AssertScoreboardEntry(state.Scoreboard, room.AliceId, "Alice", 3);
        AssertScoreboardEntry(state.Scoreboard, room.BobId, "Bob", 4);
        AssertScoreboardEntry(state.Scoreboard, room.CarlaId, "Carla", 0);

        Assert.Equal(room.RevealState.RevealedCards.Count, state.RevealedCards.Count);
        Assert.Equal(
            room.RevealState.RevealedCards.Select(
                card => (card.VotingCardId, card.CardId, card.DisplayNumber)),
            state.RevealedCards.Select(
                card => (card.VotingCardId, card.CardId, card.DisplayNumber)));

        var storytellerCard = state.RevealedCards.Single(
            card => card.CardId == room.StorytellerCardId);
        Assert.Equal(room.AliceId, storytellerCard.OwnerPlayerId);
        Assert.Equal("Alice", storytellerCard.OwnerName);
        Assert.True(storytellerCard.IsStorytellerCard);
        Assert.Collection(
            storytellerCard.Votes,
            vote =>
            {
                Assert.Equal(room.BobId, vote.VoterPlayerId);
                Assert.Equal("Bob", vote.VoterName);
            });

        var bobCard = state.RevealedCards.Single(card => card.CardId == room.BobCardId);
        Assert.Equal(room.BobId, bobCard.OwnerPlayerId);
        Assert.Equal("Bob", bobCard.OwnerName);
        Assert.False(bobCard.IsStorytellerCard);
        Assert.Collection(
            bobCard.Votes,
            vote =>
            {
                Assert.Equal(room.CarlaId, vote.VoterPlayerId);
                Assert.Equal("Carla", vote.VoterName);
            });

        var carlaCard = state.RevealedCards.Single(card => card.CardId == room.CarlaCardId);
        Assert.Equal(room.CarlaId, carlaCard.OwnerPlayerId);
        Assert.Equal("Carla", carlaCard.OwnerName);
        Assert.False(carlaCard.IsStorytellerCard);
        Assert.Empty(carlaCard.Votes);
    }

    [Fact]
    public async Task PostScore_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.PostAsync("/rooms/missing/score", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Fact]
    public async Task PostScore_WhenNotInRevealOrAlreadyScored_ReturnsBadRequestWithoutDuplicatingPoints()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        await CreateRoomAsync(client, "NOT-READY");

        var prematureResponse = await client.PostAsync(
            "/rooms/NOT-READY/score",
            content: null);

        Assert.Equal(HttpStatusCode.BadRequest, prematureResponse.StatusCode);

        var room = await PrepareRevealRoomAsync(client, "ABC123");
        var firstResponse = await client.PostAsync("/rooms/ABC123/score", content: null);
        firstResponse.EnsureSuccessStatusCode();

        var duplicateResponse = await client.PostAsync("/rooms/ABC123/score", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);

        var stateResponse = await client.GetAsync("/rooms/ABC123");
        stateResponse.EnsureSuccessStatusCode();
        var state = await ReadRequiredJsonAsync<RoomStateResponse>(stateResponse);

        Assert.Equal(RoomPhase.Scoring, state.Phase);
        AssertPlayerRoundScore(state.RoundScores, room.AliceId, "Alice", 3);
        AssertPlayerRoundScore(state.RoundScores, room.BobId, "Bob", 4);
        AssertPlayerRoundScore(state.RoundScores, room.CarlaId, "Carla", 0);
        AssertScoreboardEntry(state.Scoreboard, room.AliceId, "Alice", 3);
        AssertScoreboardEntry(state.Scoreboard, room.BobId, "Bob", 4);
        AssertScoreboardEntry(state.Scoreboard, room.CarlaId, "Carla", 0);
    }

    [Fact]
    public async Task PostCompleteRound_WithScoringRoom_ReturnsRoundCompleteAndPreservesRoundResults()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var scoringState = await PrepareScoringRoomAsync(client, "ABC123");

        var response = await client.PostAsync(
            "/rooms/abc123/complete-round",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var state = await ReadRequiredJsonAsync<RoomStateResponse>(response);
        Assert.Equal(RoomPhase.RoundComplete, state.Phase);
        Assert.Equal(scoringState.RoundScores, state.RoundScores);
        Assert.Equal(scoringState.Scoreboard, state.Scoreboard);
        Assert.Equal(
            JsonSerializer.Serialize(scoringState.RevealedCards, JsonSerializerOptions.Web),
            JsonSerializer.Serialize(state.RevealedCards, JsonSerializerOptions.Web));
        Assert.Equal(scoringState.CurrentStoryteller, state.CurrentStoryteller);
        Assert.Empty(state.Winners);
    }

    [Fact]
    public async Task PostCompleteRound_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.PostAsync(
            "/rooms/missing/complete-round",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Fact]
    public async Task PostCompleteRound_BeforeScoringOrTwice_ReturnsBadRequestWithoutChangingScores()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        await CreateRoomAsync(client, "NOT-READY");

        var prematureResponse = await client.PostAsync(
            "/rooms/NOT-READY/complete-round",
            content: null);

        Assert.Equal(HttpStatusCode.BadRequest, prematureResponse.StatusCode);

        var scoringState = await PrepareScoringRoomAsync(client, "ABC123");
        var firstResponse = await client.PostAsync(
            "/rooms/ABC123/complete-round",
            content: null);
        firstResponse.EnsureSuccessStatusCode();

        var duplicateResponse = await client.PostAsync(
            "/rooms/ABC123/complete-round",
            content: null);

        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);

        var stateResponse = await client.GetAsync("/rooms/ABC123");
        stateResponse.EnsureSuccessStatusCode();
        var state = await ReadRequiredJsonAsync<RoomStateResponse>(stateResponse);

        Assert.Equal(RoomPhase.RoundComplete, state.Phase);
        Assert.Equal(scoringState.RoundScores, state.RoundScores);
        Assert.Equal(scoringState.Scoreboard, state.Scoreboard);
    }

    [Fact]
    public async Task GetRoom_ReturnsPublicRoomStateWithoutPrivateHands()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        await CreateRoomAsync(client, "ABC123");
        await JoinPlayerAsync(client, "ABC123", Guid.NewGuid(), "Alice");
        await JoinPlayerAsync(client, "ABC123", Guid.NewGuid(), "Bob");
        await JoinPlayerAsync(client, "ABC123", Guid.NewGuid(), "Carla");
        await client.PostAsync("/rooms/ABC123/start", content: null);

        var response = await client.GetAsync("/rooms/abc123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var room = ReadRequiredJson<RoomState>(responseBody);
        Assert.Equal("ABC123", room.RoomCode);
        Assert.Equal(GamePhase.StorytellerChoosing, room.Phase);

        using var json = JsonDocument.Parse(responseBody);
        Assert.False(json.RootElement.TryGetProperty("hand", out _));
        Assert.DoesNotContain("TEMP-", responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetPlayerPrivateState_ReturnsPublicRoomAndPrivatePlayerSnapshot()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var bobId = Guid.NewGuid();

        await CreateRoomAsync(client, "ABC123");
        await JoinPlayerAsync(client, "ABC123", Guid.NewGuid(), "Alice");
        await JoinPlayerAsync(client, "ABC123", bobId, "Bob");
        await JoinPlayerAsync(client, "ABC123", Guid.NewGuid(), "Carla");
        await client.PostAsync("/rooms/ABC123/start", content: null);

        var response = await client.GetAsync($"/rooms/abc123/players/{bobId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var snapshot = ReadRequiredJson<GetPlayerPrivateStateResponse>(responseBody);

        Assert.Equal("ABC123", snapshot.Room.RoomCode);
        Assert.Equal(RoomPhase.StorytellerChoosing, snapshot.Room.Phase);
        Assert.Equal(3, snapshot.Room.Players.Count);
        Assert.Equal(bobId, snapshot.Player.PlayerId);
        Assert.Equal("Bob", snapshot.Player.Name);
        Assert.False(snapshot.Player.IsCurrentStoryteller);
        Assert.Collection(
            snapshot.Player.Hand,
            card => Assert.Equal("TEMP-007", card.CardId),
            card => Assert.Equal("TEMP-008", card.CardId),
            card => Assert.Equal("TEMP-009", card.CardId),
            card => Assert.Equal("TEMP-010", card.CardId),
            card => Assert.Equal("TEMP-011", card.CardId),
            card => Assert.Equal("TEMP-012", card.CardId));

        using var json = JsonDocument.Parse(responseBody);
        var roomJson = json.RootElement.GetProperty("room").GetRawText();
        Assert.DoesNotContain("\"hand\"", roomJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TEMP-", roomJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetPlayerPrivateState_WhenPlayerDoesNotExist_ReturnsNotFound()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        await CreateRoomAsync(client, "ABC123");

        var response = await client.GetAsync($"/rooms/ABC123/players/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    private static async Task<RoomState> CreateRoomAsync(HttpClient client, string roomCode)
    {
        var response = await client.PostAsJsonAsync("/rooms", new
        {
            RoomCode = roomCode
        });

        response.EnsureSuccessStatusCode();
        return await ReadRequiredJsonAsync<RoomState>(response);
    }

    private static async Task<RoomState> JoinPlayerAsync(
        HttpClient client,
        string roomCode,
        Guid playerId,
        string playerName)
    {
        var response = await client.PostAsJsonAsync($"/rooms/{roomCode}/players", new
        {
            PlayerId = playerId,
            PlayerName = playerName
        });

        response.EnsureSuccessStatusCode();
        return await ReadRequiredJsonAsync<RoomState>(response);
    }

    private static async Task CreateStartedRoomAsync(
        HttpClient client,
        string roomCode,
        Guid storytellerId,
        Guid? secondPlayerId = null)
    {
        await CreateRoomAsync(client, roomCode);
        await JoinPlayerAsync(client, roomCode, storytellerId, "Alice");
        await JoinPlayerAsync(client, roomCode, secondPlayerId ?? Guid.NewGuid(), "Bob");
        await JoinPlayerAsync(client, roomCode, Guid.NewGuid(), "Carla");

        var response = await client.PostAsync($"/rooms/{roomCode}/start", content: null);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<GuessSubmissionRoom> PrepareGuessSubmissionRoomAsync(
        HttpClient client,
        string roomCode)
    {
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();
        var carlaId = Guid.NewGuid();

        await CreateRoomAsync(client, roomCode);
        await JoinPlayerAsync(client, roomCode, aliceId, "Alice");
        await JoinPlayerAsync(client, roomCode, bobId, "Bob");
        await JoinPlayerAsync(client, roomCode, carlaId, "Carla");

        var startResponse = await client.PostAsync($"/rooms/{roomCode}/start", content: null);
        startResponse.EnsureSuccessStatusCode();

        var aliceState = await GetPlayerPrivateStateAsync(client, roomCode, aliceId);
        var bobState = await GetPlayerPrivateStateAsync(client, roomCode, bobId);
        var carlaState = await GetPlayerPrivateStateAsync(client, roomCode, carlaId);
        var storytellerCardId = aliceState.Player.Hand.First().CardId;

        var storytellerResponse = await client.PostAsJsonAsync(
            $"/rooms/{roomCode}/storyteller-card",
            new ChooseStorytellerCardRequest(
                aliceId,
                storytellerCardId,
                "A strange doorway"));
        storytellerResponse.EnsureSuccessStatusCode();

        return new GuessSubmissionRoom(
            aliceId,
            bobId,
            carlaId,
            storytellerCardId,
            bobState.Player.Hand.Select(card => card.CardId).ToArray(),
            carlaState.Player.Hand.Select(card => card.CardId).ToArray());
    }

    private static async Task<VotingRoom> PrepareVotingRoomAsync(
        HttpClient client,
        string roomCode)
    {
        var room = await PrepareGuessSubmissionRoomAsync(client, roomCode);
        var bobCardId = room.BobCardIds[0];
        var carlaCardId = room.CarlaCardIds[0];

        var firstResponse = await client.PostAsJsonAsync(
            $"/rooms/{roomCode}/guess-cards",
            new SubmitGuessCardRequest(room.BobId, bobCardId));
        firstResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            $"/rooms/{roomCode}/guess-cards",
            new SubmitGuessCardRequest(room.CarlaId, carlaCardId));
        response.EnsureSuccessStatusCode();

        return new VotingRoom(
            room.AliceId,
            room.BobId,
            room.CarlaId,
            room.StorytellerCardId,
            bobCardId,
            carlaCardId,
            await ReadRequiredJsonAsync<RoomStateResponse>(response));
    }

    private static async Task<RevealRoom> PrepareRevealRoomAsync(
        HttpClient client,
        string roomCode)
    {
        var room = await PrepareVotingRoomAsync(client, roomCode);
        var storytellerVotingCardId = room.VotingState.VotingCards
            .Single(card => card.CardId == room.StorytellerCardId)
            .VotingCardId;
        var bobVotingCardId = room.VotingState.VotingCards
            .Single(card => card.CardId == room.BobCardId)
            .VotingCardId;

        var firstResponse = await client.PostAsJsonAsync(
            $"/rooms/{roomCode}/votes",
            new CastVoteRequest(room.BobId, storytellerVotingCardId));
        firstResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            $"/rooms/{roomCode}/votes",
            new CastVoteRequest(room.CarlaId, bobVotingCardId));
        response.EnsureSuccessStatusCode();

        return new RevealRoom(
            room.AliceId,
            room.BobId,
            room.CarlaId,
            room.StorytellerCardId,
            room.BobCardId,
            room.CarlaCardId,
            await ReadRequiredJsonAsync<RoomStateResponse>(response));
    }

    private static async Task<RoomStateResponse> PrepareScoringRoomAsync(
        HttpClient client,
        string roomCode)
    {
        await PrepareRevealRoomAsync(client, roomCode);

        var response = await client.PostAsync(
            $"/rooms/{roomCode}/score",
            content: null);
        response.EnsureSuccessStatusCode();

        return await ReadRequiredJsonAsync<RoomStateResponse>(response);
    }

    private static void AssertPlayerRoundScore(
        IReadOnlyCollection<RoundScoreStateResponse> roundScores,
        Guid playerId,
        string playerName,
        int pointsAwarded)
    {
        var score = roundScores.Single(score => score.PlayerId == playerId);
        Assert.Equal(playerName, score.PlayerName);
        Assert.Equal(pointsAwarded, score.PointsAwarded);
    }

    private static void AssertScoreboardEntry(
        IReadOnlyCollection<ScoreboardEntryStateResponse> scoreboard,
        Guid playerId,
        string playerName,
        int totalPoints)
    {
        var score = scoreboard.Single(score => score.PlayerId == playerId);
        Assert.Equal(playerName, score.PlayerName);
        Assert.Equal(totalPoints, score.TotalPoints);
    }

    private static async Task<GetPlayerPrivateStateResponse> GetPlayerPrivateStateAsync(
        HttpClient client,
        string roomCode,
        Guid playerId)
    {
        var response = await client.GetAsync($"/rooms/{roomCode}/players/{playerId}");
        response.EnsureSuccessStatusCode();
        return await ReadRequiredJsonAsync<GetPlayerPrivateStateResponse>(response);
    }

    private static async Task<T> ReadRequiredJsonAsync<T>(HttpResponseMessage response)
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        return ReadRequiredJson<T>(responseBody);
    }

    private static T ReadRequiredJson<T>(string responseBody)
    {
        var value = JsonSerializer.Deserialize<T>(responseBody, JsonSerializerOptions.Web);
        Assert.NotNull(value);
        return value;
    }

    private sealed record VotingRoom(
        Guid AliceId,
        Guid BobId,
        Guid CarlaId,
        string StorytellerCardId,
        string BobCardId,
        string CarlaCardId,
        RoomStateResponse VotingState);

    private sealed record RevealRoom(
        Guid AliceId,
        Guid BobId,
        Guid CarlaId,
        string StorytellerCardId,
        string BobCardId,
        string CarlaCardId,
        RoomStateResponse RevealState);

    private sealed record GuessSubmissionRoom(
        Guid AliceId,
        Guid BobId,
        Guid CarlaId,
        string StorytellerCardId,
        IReadOnlyList<string> BobCardIds,
        IReadOnlyList<string> CarlaCardIds);
}
