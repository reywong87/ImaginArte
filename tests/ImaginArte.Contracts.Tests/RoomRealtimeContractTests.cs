using ImaginArte.Contracts.Rooms;

namespace ImaginArte.Contracts.Tests;

public sealed class RoomRealtimeContractTests
{
    [Fact]
    public void RoomUpdatedEvent_ContainsRoomStateResponse()
    {
        var property = typeof(RoomUpdatedEvent).GetProperty(nameof(RoomUpdatedEvent.Room));

        Assert.NotNull(property);
        Assert.Equal(typeof(RoomStateResponse), property.PropertyType);
    }

    [Fact]
    public void PlayerStateUpdatedEvent_ContainsGetPlayerPrivateStateResponse()
    {
        var property = typeof(PlayerStateUpdatedEvent).GetProperty(nameof(PlayerStateUpdatedEvent.State));

        Assert.NotNull(property);
        Assert.Equal(typeof(GetPlayerPrivateStateResponse), property.PropertyType);
    }

    [Fact]
    public void RoomStateResponse_ExposesLobbyLimitsAndHost()
    {
        Assert.Equal(
            typeof(int),
            typeof(RoomStateResponse).GetProperty(nameof(RoomStateResponse.MinimumPlayers))?.PropertyType);
        Assert.Equal(
            typeof(int),
            typeof(RoomStateResponse).GetProperty(nameof(RoomStateResponse.MaximumPlayers))?.PropertyType);
        Assert.Equal(
            typeof(Guid?),
            typeof(RoomStateResponse).GetProperty(nameof(RoomStateResponse.HostPlayerId))?.PropertyType);
        Assert.Equal(
            typeof(bool),
            typeof(PlayerStateResponse).GetProperty(nameof(PlayerStateResponse.IsConnected))?.PropertyType);
        Assert.Equal(
            typeof(string),
            typeof(PlayerStateResponse).GetProperty(nameof(PlayerStateResponse.AvatarId))?.PropertyType);
        Assert.Equal(
            typeof(string),
            typeof(JoinRoomRequest).GetProperty(nameof(JoinRoomRequest.AvatarId))?.PropertyType);
        Assert.True(typeof(JoinRoomRequest).GetConstructors().Single().GetParameters()[2].IsOptional);
    }

    [Fact]
    public void UpdateRoomTargetScoreRequest_ContainsTargetScore()
    {
        var property = typeof(UpdateRoomTargetScoreRequest)
            .GetProperty(nameof(UpdateRoomTargetScoreRequest.TargetScore));

        Assert.NotNull(property);
        Assert.Equal(typeof(int), property.PropertyType);
        Assert.Single(typeof(UpdateRoomTargetScoreRequest).GetProperties());
    }

    [Fact]
    public void RoomPhase_VoteRevealIsAppendedWithoutChangingExistingValues()
    {
        Assert.Equal(3, (int)RoomPhase.Voting);
        Assert.Equal(4, (int)RoomPhase.Reveal);
        Assert.Equal(7, (int)RoomPhase.GameComplete);
        Assert.Equal(8, (int)RoomPhase.VoteReveal);
    }

    [Fact]
    public void PublicRoomSnapshot_DoesNotExposePrivateHands()
    {
        var publicSnapshotTypes = new[]
        {
            typeof(RoomStateResponse),
            typeof(PlayerStateResponse),
            typeof(VotingCardStateResponse),
            typeof(VotingCardVoteStateResponse),
            typeof(RevealCardStateResponse),
            typeof(RevealedVoteStateResponse),
            typeof(RoundScoreStateResponse),
            typeof(ScoreboardEntryStateResponse),
            typeof(WinningPlayerStateResponse)
        };

        Assert.DoesNotContain(
            publicSnapshotTypes.SelectMany(type => type.GetProperties()),
            property => property.Name.Contains("Hand", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(PlayerPrivateStateResponse), publicSnapshotTypes);
    }

    [Fact]
    public void EventNames_AreStableAndDistinct()
    {
        Assert.Equal("RoomUpdated", RoomRealtimeEventNames.RoomUpdated);
        Assert.Equal("PlayerStateUpdated", RoomRealtimeEventNames.PlayerStateUpdated);
        Assert.NotEqual(
            RoomRealtimeEventNames.RoomUpdated,
            RoomRealtimeEventNames.PlayerStateUpdated);
    }

    [Fact]
    public void ContractsAssembly_DoesNotDependOnInternalLayersOrSignalR()
    {
        var forbiddenDependencyNames = new[]
        {
            "ImaginArte.Domain",
            "ImaginArte.Application",
            "ImaginArte.Server",
            "ImaginArte.Infrastructure",
            "ImaginArte.Client.Blazor",
            "Microsoft.AspNetCore",
            "Microsoft.AspNetCore.SignalR"
        };
        var referencedAssemblyNames = typeof(RoomUpdatedEvent).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray();

        Assert.DoesNotContain(
            referencedAssemblyNames,
            reference => forbiddenDependencyNames.Any(
                forbidden => reference.Equals(forbidden, StringComparison.Ordinal)
                    || reference.StartsWith($"{forbidden}.", StringComparison.Ordinal)));
        Assert.DoesNotContain(
            referencedAssemblyNames,
            reference => reference.Contains("Blazor", StringComparison.OrdinalIgnoreCase)
                || reference.Contains("SignalR", StringComparison.OrdinalIgnoreCase));
    }
}
