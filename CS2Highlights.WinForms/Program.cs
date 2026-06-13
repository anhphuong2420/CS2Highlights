using CS2Highlights.Core.Interfaces;
using CS2Highlights.Database;
using CS2Highlights.DemoScanner;
using CS2Highlights.Parser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Text.Json;


namespace CS2Highlights.WinForms;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var (dbPath, logFolder) = ReadConfig();

        Directory.CreateDirectory(logFolder);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(logFolder, "cs2highlights-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("CS2Highlights starting");

        var services = new ServiceCollection();

        services.AddDbContextFactory<AppDbContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"));

        services.AddSingleton<SettingsRepository>();
        services.AddSingleton<DemoFolderScanner>();
        services.AddSingleton<LightweightDemoReader>();
        services.AddSingleton<DemoParser>();

        services.AddSingleton<IHighlightDetector, MultiKillDetector>();
        services.AddSingleton<IHighlightDetector, ClutchDetector>();
        services.AddSingleton<HighlightService>();

        services.AddTransient<MainForm>();
        services.AddTransient<SettingsPanel>();

        var provider = services.BuildServiceProvider();

        // Ensure DB schema is up to date
        var factory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using (var db = factory.CreateDbContext())
            db.Database.Migrate();

        Application.Run(provider.GetRequiredService<MainForm>());

        Log.Information("CS2Highlights shutting down");
        Log.CloseAndFlush();
    }

    private static (string dbPath, string logFolder) ReadConfig()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var defaultDb = Path.Combine(baseDir, "cs2highlights.db");
        var defaultLog = Path.Combine(baseDir, "logs");

        var configPath = Path.Combine(baseDir, "appsettings.json");
        if (!File.Exists(configPath))
            return (defaultDb, defaultLog);

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
            var root = doc.RootElement;

            var dbPath = root.TryGetProperty("Database", out var dbSection)
                && dbSection.TryGetProperty("Path", out var dbPathProp)
                ? dbPathProp.GetString() ?? defaultDb
                : defaultDb;

            var logFolder = root.TryGetProperty("Logging", out var logSection)
                && logSection.TryGetProperty("LogFolder", out var logProp)
                ? logProp.GetString() ?? defaultLog
                : defaultLog;

            return (dbPath, logFolder);
        }
        catch
        {
            return (defaultDb, defaultLog);
        }
    }
}
