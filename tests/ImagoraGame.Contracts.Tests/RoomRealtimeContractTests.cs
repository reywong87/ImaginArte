using ImagoraGame.Contracts.Rooms;

namespace ImagoraGame.Contracts.Tests;

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
            "ImagoraGame.Domain",
            "ImagoraGame.Application",
            "ImagoraGame.Server",
            "ImagoraGame.Infrastructure",
            "ImagoraGame.Client.Blazor",
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
