using System.Net.Http.Json;
using System.Text.Json;
using ImagoraGame.Contracts.Rooms;

namespace ImagoraGame.Client.Blazor.Rooms;

public sealed class MobileRoomClient : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly RoomClientOptions _options;
    private readonly IRoomRealtimeConnectionFactory _connectionFactory;
    private IRoomRealtimeConnection? _connection;
    private IDisposable? _playerStateUpdatedSubscription;
    private MobileLobbyState? _lobby;
    private bool _disposed;

    public MobileRoomClient(
        HttpClient httpClient,
        RoomClientOptions options,
        IRoomRealtimeConnectionFactory connectionFactory)
    {
        _httpClient = httpClient;
        _options = options;
        _connectionFactory = connectionFactory;
    }

    public event Action? StateChanged;

    public MobileLobbyState? Lobby => _lobby;

    public GetPlayerPrivateStateResponse? PrivateState { get; private set; }

    public RoomConnectionStatus ConnectionStatus { get; private set; } =
        RoomConnectionStatus.Loading;

    public string? ErrorMessage { get; private set; }

    public bool IsChoosingStorytellerCard { get; private set; }

    public bool IsSubmittingGuessCard { get; private set; }

    public bool IsCastingVote { get; private set; }

    public bool HasSubmittedGuessCard =>
        PrivateState?.Player.HasSubmittedGuessCard == true;

    public bool IsOwnSubmittedVotingCard(string votingCardId)
    {
        return PrivateState?.Player.SubmittedVotingCardId == votingCardId;
    }

    public bool CanVoteForCard(string votingCardId)
    {
        return !string.IsNullOrWhiteSpace(votingCardId) &&
            !IsOwnSubmittedVotingCard(votingCardId);
    }

    public async Task ConnectAsync(
        MobileLobbyState lobby,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_connection is not null)
        {
            throw new InvalidOperationException("The mobile room client is already connected.");
        }

        _lobby = lobby;
        PrivateState = null;
        ErrorMessage = null;
        SetStatus(RoomConnectionStatus.Loading);

        try
        {
            var hubUri = new Uri(_options.ApiBaseUri, "hubs/rooms");
            _connection = _connectionFactory.Create(hubUri);
            _playerStateUpdatedSubscription =
                _connection.OnPlayerStateUpdated(HandlePlayerStateUpdated);
            _connection.Reconnecting += HandleReconnectingAsync;
            _connection.Reconnected += HandleReconnectedAsync;
            _connection.Closed += HandleClosedAsync;

            await _connection.StartAsync(cancellationToken);
            await _connection.SubscribeAsPlayerAsync(
                lobby.RoomCode,
                lobby.PlayerId,
                cancellationToken);
            SetStatus(RoomConnectionStatus.Connected);
        }
        catch (Exception exception)
        {
            ErrorMessage = await ToUserMessageAsync(exception);
            SetStatus(RoomConnectionStatus.Error);
        }
    }

    public async Task ChooseStorytellerCardAsync(
        string cardId,
        string clue,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_lobby is null || IsChoosingStorytellerCard)
        {
            return;
        }

        IsChoosingStorytellerCard = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                $"rooms/{Uri.EscapeDataString(_lobby.RoomCode.Trim())}/storyteller-card",
                new ChooseStorytellerCardRequest(
                    _lobby.PlayerId,
                    cardId,
                    clue.Trim()),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw await CreateRequestExceptionAsync(response, cancellationToken);
            }

            var room = await response.Content.ReadFromJsonAsync<RoomStateResponse>(
                cancellationToken: cancellationToken)
                ?? throw new HttpRequestException("El servidor devolvió una respuesta vacía.");

            if (PrivateState is not null)
            {
                PrivateState = PrivateState with { Room = room };
            }
        }
        catch (Exception exception)
        {
            ErrorMessage = await ToUserMessageAsync(exception);
        }
        finally
        {
            IsChoosingStorytellerCard = false;
            NotifyStateChanged();
        }
    }

    public async Task SubmitGuessCardAsync(
        string cardId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_lobby is null || IsSubmittingGuessCard)
        {
            return;
        }

        IsSubmittingGuessCard = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                $"rooms/{Uri.EscapeDataString(_lobby.RoomCode.Trim())}/guess-cards",
                new SubmitGuessCardRequest(
                    _lobby.PlayerId,
                    cardId),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw await CreateRequestExceptionAsync(response, cancellationToken);
            }

            var room = await response.Content.ReadFromJsonAsync<RoomStateResponse>(
                cancellationToken: cancellationToken)
                ?? throw new HttpRequestException("El servidor devolvió una respuesta vacía.");

            if (PrivateState is not null)
            {
                PrivateState = PrivateState with
                {
                    Room = room,
                    Player = PrivateState.Player with { HasSubmittedGuessCard = true }
                };
            }
        }
        catch (Exception exception)
        {
            ErrorMessage = await ToUserMessageAsync(exception);
        }
        finally
        {
            IsSubmittingGuessCard = false;
            NotifyStateChanged();
        }
    }

    public async Task CastVoteAsync(
        string votingCardId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_lobby is null || IsCastingVote)
        {
            return;
        }

        IsCastingVote = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                $"rooms/{Uri.EscapeDataString(_lobby.RoomCode.Trim())}/votes",
                new CastVoteRequest(
                    _lobby.PlayerId,
                    votingCardId),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw await CreateRequestExceptionAsync(response, cancellationToken);
            }

            var room = await response.Content.ReadFromJsonAsync<RoomStateResponse>(
                cancellationToken: cancellationToken)
                ?? throw new HttpRequestException("El servidor devolvió una respuesta vacía.");

            if (PrivateState is not null)
            {
                PrivateState = PrivateState with { Room = room };
            }
        }
        catch (Exception exception)
        {
            ErrorMessage = await ToUserMessageAsync(exception);
        }
        finally
        {
            IsCastingVote = false;
            NotifyStateChanged();
        }
    }

    private void HandlePlayerStateUpdated(PlayerStateUpdatedEvent playerStateUpdated)
    {
        PrivateState = playerStateUpdated.State;
        ErrorMessage = null;
        NotifyStateChanged();
    }

    private Task HandleReconnectingAsync(Exception? exception)
    {
        SetStatus(RoomConnectionStatus.Reconnecting);
        return Task.CompletedTask;
    }

    private async Task HandleReconnectedAsync(string? connectionId)
    {
        if (_connection is null || _lobby is null)
        {
            return;
        }

        try
        {
            await _connection.SubscribeAsPlayerAsync(
                _lobby.RoomCode,
                _lobby.PlayerId,
                CancellationToken.None);
            ErrorMessage = null;
            SetStatus(RoomConnectionStatus.Connected);
        }
        catch (Exception exception)
        {
            ErrorMessage = await ToUserMessageAsync(exception);
            SetStatus(RoomConnectionStatus.Error);
        }
    }

    private Task HandleClosedAsync(Exception? exception)
    {
        if (_disposed)
        {
            return Task.CompletedTask;
        }

        ErrorMessage = exception is null
            ? null
            : "Se perdió la conexión en tiempo real con la sala.";
        SetStatus(RoomConnectionStatus.Disconnected);
        return Task.CompletedTask;
    }

    private void SetStatus(RoomConnectionStatus status)
    {
        ConnectionStatus = status;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
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

    private static Task<string> ToUserMessageAsync(Exception exception)
    {
        var message = exception switch
        {
            HttpRequestException => exception.Message,
            OperationCanceledException => "La operación fue cancelada.",
            _ => "No se pudo comunicar con la sala. Comprueba que el servidor está disponible."
        };

        return Task.FromResult(message);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StateChanged = null;
        _playerStateUpdatedSubscription?.Dispose();

        if (_connection is not null)
        {
            _connection.Reconnecting -= HandleReconnectingAsync;
            _connection.Reconnected -= HandleReconnectedAsync;
            _connection.Closed -= HandleClosedAsync;
            await _connection.DisposeAsync();
        }
    }
}
