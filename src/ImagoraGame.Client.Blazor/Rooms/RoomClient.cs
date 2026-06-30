using System.Net.Http.Json;
using System.Text.Json;
using ImagoraGame.Contracts.Rooms;

namespace ImagoraGame.Client.Blazor.Rooms;

public sealed class RoomClient : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly RoomClientOptions _options;
    private readonly IRoomRealtimeConnectionFactory _connectionFactory;
    private IRoomRealtimeConnection? _connection;
    private IDisposable? _roomUpdatedSubscription;
    private string? _roomCode;
    private bool _disposed;

    public RoomClient(
        HttpClient httpClient,
        RoomClientOptions options,
        IRoomRealtimeConnectionFactory connectionFactory)
    {
        _httpClient = httpClient;
        _options = options;
        _connectionFactory = connectionFactory;
    }

    public event Action? StateChanged;

    public RoomStateResponse? Room { get; private set; }

    public RoomConnectionStatus ConnectionStatus { get; private set; } =
        RoomConnectionStatus.Loading;

    public string? ErrorMessage { get; private set; }

    public bool IsStarting { get; private set; }

    public bool IsScoringRound { get; private set; }

    public bool IsCompletingRound { get; private set; }

    public bool IsAdvancingRound { get; private set; }

    public async Task InitializeAsync(string roomCode, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_connection is not null)
        {
            throw new InvalidOperationException("The room client is already initialized.");
        }

        _roomCode = roomCode;
        SetStatus(RoomConnectionStatus.Loading);

        try
        {
            Room = await GetRoomAsync(roomCode, cancellationToken);
            NotifyStateChanged();

            var hubUri = new Uri(_options.ApiBaseUri, "hubs/rooms");
            _connection = _connectionFactory.Create(hubUri);
            _roomUpdatedSubscription = _connection.OnRoomUpdated(HandleRoomUpdated);
            _connection.Reconnecting += HandleReconnectingAsync;
            _connection.Reconnected += HandleReconnectedAsync;
            _connection.Closed += HandleClosedAsync;

            await _connection.StartAsync(cancellationToken);
            await _connection.SubscribeToRoomAsync(roomCode, cancellationToken);
            SetStatus(RoomConnectionStatus.Connected);
        }
        catch (Exception exception)
        {
            ErrorMessage = await ToUserMessageAsync(exception);
            SetStatus(RoomConnectionStatus.Error);
        }
    }

    public async Task StartGameAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Room is null || _roomCode is null || IsStarting)
        {
            return;
        }

        IsStarting = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            using var response = await _httpClient.PostAsync(
                $"rooms/{Uri.EscapeDataString(_roomCode.Trim())}/start",
                content: null,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw await CreateRequestExceptionAsync(response, cancellationToken);
            }

            Room = await response.Content.ReadFromJsonAsync<RoomStateResponse>(
                cancellationToken: cancellationToken)
                ?? throw new HttpRequestException("El servidor devolvió una respuesta vacía.");
        }
        catch (Exception exception)
        {
            ErrorMessage = await ToUserMessageAsync(exception);
        }
        finally
        {
            IsStarting = false;
            NotifyStateChanged();
        }
    }

    public async Task ScoreRoundAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Room is null || _roomCode is null || IsScoringRound)
        {
            return;
        }

        IsScoringRound = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            using var response = await _httpClient.PostAsync(
                $"rooms/{Uri.EscapeDataString(_roomCode.Trim())}/score",
                content: null,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw await CreateRequestExceptionAsync(response, cancellationToken);
            }

            Room = await response.Content.ReadFromJsonAsync<RoomStateResponse>(
                cancellationToken: cancellationToken)
                ?? throw new HttpRequestException("El servidor devolvió una respuesta vacía.");
        }
        catch (Exception exception)
        {
            ErrorMessage = await ToUserMessageAsync(exception);
        }
        finally
        {
            IsScoringRound = false;
            NotifyStateChanged();
        }
    }

    public async Task CompleteRoundAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Room is null || _roomCode is null || IsCompletingRound)
        {
            return;
        }

        IsCompletingRound = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            using var response = await _httpClient.PostAsync(
                $"rooms/{Uri.EscapeDataString(_roomCode.Trim())}/complete-round",
                content: null,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw await CreateRequestExceptionAsync(response, cancellationToken);
            }

            Room = await response.Content.ReadFromJsonAsync<RoomStateResponse>(
                cancellationToken: cancellationToken)
                ?? throw new HttpRequestException("El servidor devolvió una respuesta vacía.");
        }
        catch (Exception exception)
        {
            ErrorMessage = await ToUserMessageAsync(exception);
        }
        finally
        {
            IsCompletingRound = false;
            NotifyStateChanged();
        }
    }

    public async Task AdvanceRoundAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Room is null || _roomCode is null || IsAdvancingRound)
        {
            return;
        }

        IsAdvancingRound = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            using var response = await _httpClient.PostAsync(
                $"rooms/{Uri.EscapeDataString(_roomCode.Trim())}/advance-round",
                content: null,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw await CreateRequestExceptionAsync(response, cancellationToken);
            }

            Room = await response.Content.ReadFromJsonAsync<RoomStateResponse>(
                cancellationToken: cancellationToken)
                ?? throw new HttpRequestException("El servidor devolvió una respuesta vacía.");
        }
        catch (Exception exception)
        {
            ErrorMessage = await ToUserMessageAsync(exception);
        }
        finally
        {
            IsAdvancingRound = false;
            NotifyStateChanged();
        }
    }

    private async Task<RoomStateResponse> GetRoomAsync(
        string roomCode,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            $"rooms/{Uri.EscapeDataString(roomCode.Trim())}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateRequestExceptionAsync(response, cancellationToken);
        }

        return await response.Content.ReadFromJsonAsync<RoomStateResponse>(
            cancellationToken: cancellationToken)
            ?? throw new HttpRequestException("El servidor devolvió una respuesta vacía.");
    }

    private void HandleRoomUpdated(RoomUpdatedEvent roomUpdated)
    {
        Room = roomUpdated.Room;
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
        if (_connection is null || _roomCode is null)
        {
            return;
        }

        try
        {
            await _connection.SubscribeToRoomAsync(_roomCode, CancellationToken.None);
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
        _roomUpdatedSubscription?.Dispose();

        if (_connection is not null)
        {
            _connection.Reconnecting -= HandleReconnectingAsync;
            _connection.Reconnected -= HandleReconnectedAsync;
            _connection.Closed -= HandleClosedAsync;
            await _connection.DisposeAsync();
        }
    }
}
