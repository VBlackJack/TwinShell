using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Services;
using TwinShell.Infrastructure.Services;
using TwinShell.Persistence;
using TwinShell.Persistence.Repositories;
using TwinShell.App.ViewModels;
using TwinShell.App.Views;

namespace TwinShell.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Apply migrations and seed data
        InitializeDatabaseAsync().ConfigureAwait(false);

        // Create and show main window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Database
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TwinShell",
            "twinshell.db");

        var dbDirectory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        services.AddDbContext<TwinShellDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Repositories
        services.AddScoped<IActionRepository, ActionRepository>();
        services.AddScoped<ICommandHistoryRepository, CommandHistoryRepository>();
        services.AddScoped<IFavoritesRepository, FavoritesRepository>();

        // Core Services
        services.AddScoped<IActionService, ActionService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<ICommandGeneratorService, CommandGeneratorService>();
        services.AddScoped<ICommandHistoryService, CommandHistoryService>();
        services.AddScoped<IFavoritesService, FavoritesService>();
        services.AddScoped<IConfigurationService, ConfigurationService>();

        // Infrastructure Services
        services.AddSingleton<IClipboardService, ClipboardService>();

        // Seed Service
        var seedFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "seed", "initial-actions.json");
        services.AddScoped<ISeedService>(sp =>
            new JsonSeedService(sp.GetRequiredService<IActionRepository>(), seedFilePath));

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<RecentCommandsViewModel>();

        // Views
        services.AddTransient<HistoryPanel>();
        services.AddTransient<RecentCommandsWidget>();

        // Windows
        services.AddTransient<MainWindow>();
    }

    private async Task InitializeDatabaseAsync()
    {
        using var scope = _serviceProvider!.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TwinShellDbContext>();

        // Apply migrations
        await context.Database.MigrateAsync();

        // Seed initial data
        var seedService = scope.ServiceProvider.GetRequiredService<ISeedService>();
        await seedService.SeedAsync();

        // Cleanup old history entries (90 days retention)
        var historyService = scope.ServiceProvider.GetRequiredService<ICommandHistoryService>();
        await historyService.CleanupOldEntriesAsync(90);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
