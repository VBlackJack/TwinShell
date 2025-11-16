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

        // Load user settings and apply theme
        InitializeThemeAsync().ConfigureAwait(false);

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
        services.AddScoped<ICustomCategoryRepository, CustomCategoryRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IBatchRepository, BatchRepository>();

        // Core Services
        services.AddScoped<IActionService, ActionService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<ICommandGeneratorService, CommandGeneratorService>();
        services.AddScoped<ICommandHistoryService, CommandHistoryService>();
        services.AddScoped<IFavoritesService, FavoritesService>();
        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<ICustomCategoryService, CustomCategoryService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IBatchService, BatchService>();
        services.AddScoped<IBatchExecutionService, BatchExecutionService>();
        services.AddScoped<IPowerShellGalleryService, PowerShellGalleryService>();

        // Theme and Settings Services (Singletons to maintain state)
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();

        // Infrastructure Services
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<INotificationService, TwinShell.App.Services.NotificationService>();
        services.AddScoped<ICommandExecutionService, CommandExecutionService>();

        // Seed Service
        var seedFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "seed", "initial-actions.json");
        services.AddScoped<ISeedService>(sp =>
            new JsonSeedService(sp.GetRequiredService<IActionRepository>(), seedFilePath));

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<RecentCommandsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<CategoryManagementViewModel>();
        services.AddTransient<ExecutionViewModel>();
        services.AddTransient<BatchViewModel>();
        services.AddTransient<PowerShellGalleryViewModel>();

        // Views
        services.AddTransient<HistoryPanel>();
        services.AddTransient<RecentCommandsWidget>();
        services.AddTransient<OutputPanel>();
        services.AddTransient<BatchPanel>();
        services.AddTransient<PowerShellGalleryPanel>();

        // Windows
        services.AddTransient<MainWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<CategoryManagementWindow>();
    }

    private async Task InitializeThemeAsync()
    {
        var settingsService = _serviceProvider!.GetRequiredService<ISettingsService>();
        var themeService = _serviceProvider!.GetRequiredService<IThemeService>();
        var localizationService = _serviceProvider!.GetRequiredService<ILocalizationService>();

        // Load user settings
        var settings = await settingsService.LoadSettingsAsync();

        // Apply the saved theme
        themeService.ApplyTheme(settings.Theme);

        // Apply the saved language
        try
        {
            localizationService.ChangeLanguage(settings.CultureCode);
        }
        catch
        {
            // Fallback to French if culture is invalid
            localizationService.ChangeLanguage("fr");
        }
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

        // Cleanup old history entries using user's configured retention days
        var settingsService = _serviceProvider!.GetRequiredService<ISettingsService>();
        var historyService = scope.ServiceProvider.GetRequiredService<ICommandHistoryService>();
        await historyService.CleanupOldEntriesAsync(settingsService.CurrentSettings.AutoCleanupDays);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
