using System.Net.Http.Json;
using System.Text.Json;
using ImagoraGame.Contracts.Rooms;

namespace ImagoraGame.Client.Blazor.Rooms;

public sealed class MobileJoinClient(HttpClient httpClient)
{
    public async Task<MobileLobbyState> JoinAsync(
        string roomCode,
        string playerName,
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        var normalizedRoomCode = roomCode.Trim().ToUpperInvariant();
        var normalizedPlayerName = playerName.Trim();

        using var response = await httpClient.PostAsJsonAsync(
            $"rooms/{Uri.EscapeDataString(normalizedRoomCode)}/players",
            new JoinRoomRequest(playerId, normalizedPlayerName),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateRequestExceptionAsync(response, cancellationToken);
        }

        var room = await response.Content.ReadFromJsonAsync<RoomStateResponse>(
            cancellationToken: cancellationToken)
            ?? throw new HttpRequestException("El servidor devolvió una respuesta vacía.");

        var joinedPlayer = room.Players.FirstOrDefault(player => player.PlayerId == playerId);
        return new MobileLobbyState(
            room.RoomCode,
            playerId,
            joinedPlayer?.Name ?? normalizedPlayerName);
    }

    private static async Task<HttpRequestException> CreateRequestExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var detail = await ReadProblemDetailAsync(response, cancellationToken);
        return new HttpRequestException(
            string.IsNullOrWhiteSpace(detail)
                ? $"El servidor rechazó la operación ({(int)response.StatusCode})."
                : detail,
            inner: null,
            response.StatusCode);
    }

    private static async Task<string?> ReadProblemDetailAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);

            if (document.RootElement.TryGetProperty("detail", out var detail))
            {
                return detail.GetString();
            }

            if (document.RootElement.TryGetProperty("title", out var title))
            {
                return title.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
