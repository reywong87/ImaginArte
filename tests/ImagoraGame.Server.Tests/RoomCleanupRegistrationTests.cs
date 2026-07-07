using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

    private static WebApplicationFactory<Program> CreateApplication(
        string persistenceProvider,
        bool cleanupEnabled,
        string? sqliteConnectionString = null)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Persistence:Provider", persistenceProvider);
                builder.UseSetting("RoomCleanup:Enabled", cleanupEnabled.ToString());
                builder.UseSetting("RoomCleanup:InactiveRoomTtlHours", "12");
                builder.UseSetting("RoomCleanup:IntervalMinutes", "30");

                if (sqliteConnectionString is not null)
                {
                    builder.UseSetting(
                        "Persistence:ConnectionString",
                        sqliteConnectionString);
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
