using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ImagoraGame.Application.Rooms;
using ImagoraGame.Infrastructure.Persistence;

namespace ImagoraGame.Server.Tests;

public sealed class RoomCleanupRegistrationTests
{
    [Fact]
    public void SQLiteProvider_WithCleanupEnabled_RegistersCleanupServices()
    {
        using var database = TemporarySqliteDatabase.Create();
        using var application = CreateApplication(
            "SQLite",
            cleanupEnabled: true,
            database.ConnectionString);

        var hostedServices = application.Services.GetServices<IHostedService>();

        Assert.Contains(
            hostedServices,
            service => service is RoomCleanupBackgroundService);
        Assert.NotNull(application.Services.GetService<RoomCleanupRunner>());
    }

    [Fact]
    public void SQLiteProvider_WithCleanupDisabled_DoesNotRegisterCleanupServices()
    {
        using var database = TemporarySqliteDatabase.Create();
        using var application = CreateApplication(
            "SQLite",
            cleanupEnabled: false,
            database.ConnectionString);

        var hostedServices = application.Services.GetServices<IHostedService>();

        Assert.DoesNotContain(
            hostedServices,
            service => service is RoomCleanupBackgroundService);
        Assert.Null(application.Services.GetService<RoomCleanupRunner>());
    }

    [Fact]
    public void InMemoryProvider_WithCleanupEnabled_DoesNotRegisterCleanupServices()
    {
        using var application = CreateApplication(
            "InMemory",
            cleanupEnabled: true);

        var hostedServices = application.Services.GetServices<IHostedService>();

        Assert.DoesNotContain(
            hostedServices,
            service => service is RoomCleanupBackgroundService);
        Assert.Null(application.Services.GetService<RoomCleanupRunner>());
    }

    [Fact]
    public void PostgreSQLProvider_RegistersPostgresRoomRepository()
    {
        using var application = CreateApplication(
            "PostgreSQL",
            cleanupEnabled: false,
            connectionString: "Host=localhost;Database=imagora;Username=imagora;Password=imagora");

        var roomRepository = application.Services.GetRequiredService<IRoomRepository>();
        var cleanupRepository = application.Services.GetRequiredService<IRoomCleanupRepository>();

        Assert.IsType<PostgresRoomRepository>(roomRepository);
        Assert.Same(roomRepository, cleanupRepository);
    }

    [Fact]
    public void PostgreSQLProvider_WithEmptyConnectionString_FailsWithClearMessage()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var application = CreateApplication(
                "PostgreSQL",
                cleanupEnabled: false,
                connectionString: " ");

            _ = application.Services;
        });

        Assert.Contains(
            "PostgreSQL persistence requires a non-empty Persistence:ConnectionString",
            exception.Message);
        Assert.Contains("Persistence__ConnectionString", exception.Message);
    }

    [Fact]
    public void PostgreSQLProvider_WithCleanupEnabled_RegistersCleanupServices()
    {
        using var application = CreateApplication(
            "PostgreSQL",
            cleanupEnabled: true,
            connectionString: "Host=localhost;Database=imagora;Username=imagora;Password=imagora");

        var hostedServices = application.Services.GetServices<IHostedService>();

        Assert.Contains(
            hostedServices,
            service => service is RoomCleanupBackgroundService);
        Assert.NotNull(application.Services.GetService<RoomCleanupRunner>());
    }

    private static WebApplicationFactory<Program> CreateApplication(
        string persistenceProvider,
        bool cleanupEnabled,
        string? connectionString = null)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Persistence:Provider", persistenceProvider);
                builder.UseSetting("RoomCleanup:Enabled", cleanupEnabled.ToString());
                builder.UseSetting("RoomCleanup:InactiveRoomTtlHours", "12");
                builder.UseSetting("RoomCleanup:IntervalMinutes", "30");

                if (connectionString is not null)
                {
                    builder.UseSetting(
                        "Persistence:ConnectionString",
                        connectionString);
                }
            });
    }

    private sealed class TemporarySqliteDatabase : IDisposable
    {
        private TemporarySqliteDatabase(string path)
        {
            Path = path;
            ConnectionString = $"Data Source={Path}";
        }

        private string Path { get; }

        public string ConnectionString { get; }

        public static TemporarySqliteDatabase Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"imagora-{Guid.NewGuid():N}.db");

            return new TemporarySqliteDatabase(path);
        }

        public void Dispose()
        {
            DeleteIfExists(Path);
            DeleteIfExists($"{Path}-shm");
            DeleteIfExists($"{Path}-wal");
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
