using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Services;
using TwinShell.Infrastructure.Services;
using TwinShell.Persistence;
using TwinShell.Persistence.Repositories;
using TwinShell.App.ViewModels;
using TwinShell.App.Views;
using TwinShell.App.Services;

namespace TwinShell.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private readonly StartupLogger _logger = StartupLogger.Instance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Add global exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        try
        {
            _logger.LogInfo("Starting application...");

            var services = new ServiceCollection();
            _logger.LogInfo("Configuring services...");
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            _logger.LogInfo("Services configured");

            // Initialize theme and localization BEFORE creating the window
            _logger.LogInfo("Initializing theme and localization...");
            InitializeThemeAndLocalization();
            _logger.LogInfo("Theme and localization initialized");

            _logger.LogInfo("Initializing database...");
            InitializeDatabaseAsync().GetAwaiter().GetResult();
            _logger.LogInfo("Database initialized");

            // Create and show main window
            _logger.LogInfo("Creating main window...");
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            _logger.LogInfo("Main window created");

            _logger.LogInfo("Showing main window...");
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Show();
            mainWindow.Activate();
            mainWindow.Topmost = true;
            mainWindow.Topmost = false; // Set to true then false to bring to front
            _logger.LogInfo("Main window shown!");

            // Start automatic Git sync if enabled
            _ = PerformStartupGitSyncAsync();
        }
        catch (Exception ex)
        {
            _logger.LogErrorSync("Startup error", ex);
            // LOCALIZATION: Use resource strings for error messages
            var localization = _serviceProvider?.GetService<ILocalizationService>();
            var message = localization?.GetString("MessageStartupError")
                ?? "An error occurred during application startup.\n\nPlease check the startup-error.log file for details.";
            var title = localization?.GetString("DialogTitleStartupError") ?? "Startup Error";
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _logger.LogErrorSync("Unhandled exception", ex);
        }
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        _logger.LogError("Dispatcher exception", e.Exception);
        // LOCALIZATION: Use resource strings for error messages
        var localization = _serviceProvider?.GetService<ILocalizationService>();
        var message = localization?.GetString("MessageUnexpectedError")
            ?? "An unexpected error occurred.\n\nThe application will continue to run but some features may be affected.";
        var title = localization?.GetString("DialogTitleError") ?? "Error";
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Logging infrastructure for enhanced observability
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

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
        services.AddScoped<ISearchHistoryRepository, SearchHistoryRepository>();

        // Core Services
        services.AddScoped<IActionService, ActionService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<ISearchHistoryService, SearchHistoryService>();
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
        services.AddSingleton<IDialogService, TwinShell.App.Services.DialogService>();
        services.AddScoped<ICommandExecutionService, CommandExecutionService>();
        services.AddScoped<IImportExportService, ImportExportService>();
        services.AddScoped<ISyncService, JsonSyncService>();
        services.AddSingleton<IGitSyncService, GitSyncService>();

        // Seed Service
        // ARCHITECTURE FIX: Use AppData for seed files to avoid Program Files read-only issues
        var appDataSeedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TwinShell",
            "data",
            "seed",
            "initial-actions.json");

        // Ensure seed directory exists in AppData
        var appDataSeedDir = Path.GetDirectoryName(appDataSeedPath);
        if (!string.IsNullOrEmpty(appDataSeedDir) && !Directory.Exists(appDataSeedDir))
        {
            Directory.CreateDirectory(appDataSeedDir);
        }

        // Copy seed file from installation to AppData if not already present
        if (!File.Exists(appDataSeedPath))
        {
            var installSeedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "seed", "initial-actions.json");
            if (File.Exists(installSeedPath))
            {
                try
                {
                    File.Copy(installSeedPath, appDataSeedPath, overwrite: false);
                    _logger.LogInfo($"Seed file copied to AppData: {appDataSeedPath}");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to copy seed file to AppData, will use installation path as fallback", ex);
                }
            }
        }

        // Use AppData path if available, fallback to installation path
        var seedFilePath = File.Exists(appDataSeedPath)
            ? appDataSeedPath
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "seed", "initial-actions.json");

        services.AddScoped<ISeedService>(sp =>
            new JsonSeedService(sp.GetRequiredService<IActionRepository>(), seedFilePath));

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<CategoryManagementViewModel>();
        services.AddTransient<ExecutionViewModel>();
        services.AddTransient<BatchViewModel>();
        services.AddTransient<PowerShellGalleryViewModel>();
        services.AddTransient<ActionEditorViewModel>();

        // Views
        services.AddTransient<HistoryPanel>();
        services.AddTransient<OutputPanel>();
        services.AddTransient<BatchPanel>();
        services.AddTransient<PowerShellGalleryPanel>();

        // Windows
        services.AddTransient<MainWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<CategoryManagementWindow>();
        services.AddTransient<ActionEditorWindow>();
    }

    /// <summary>
    /// Initializes theme and localization synchronously.
    /// Called BEFORE window creation to ensure proper theme application.
    /// </summary>
    private void InitializeThemeAndLocalization()
    {
        if (_serviceProvider == null)
        {
            throw new InvalidOperationException("Service provider has not been initialized");
        }

        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var themeService = _serviceProvider.GetRequiredService<IThemeService>();
        var localizationService = _serviceProvider.GetRequiredService<ILocalizationService>();

        // Load user settings synchronously
        var settings = settingsService.LoadSettingsAsync().GetAwaiter().GetResult();

        // Apply the saved theme SYNCHRONOUSLY before window creation
        _logger.LogInfo($"Applying theme: {settings.Theme}");
        themeService.ApplyTheme(settings.Theme);
        _logger.LogInfo($"Theme applied successfully: {settings.Theme}");

        // Apply the saved language
        try
        {
            _logger.LogInfo($"Applying language: {settings.CultureCode}");
            localizationService.ChangeLanguage(settings.CultureCode);
            _logger.LogInfo($"Language applied successfully: {settings.CultureCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to apply language, falling back to French", ex);
            // Fallback to French if culture is invalid
            localizationService.ChangeLanguage("fr");
        }
    }

    private async Task InitializeDatabaseAsync()
    {
        // BUGFIX: Added null-check to prevent NullReferenceException if OnStartup fails before _serviceProvider is initialized
        if (_serviceProvider == null)
        {
            throw new InvalidOperationException("Service provider has not been initialized");
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TwinShellDbContext>();

        // Create database and tables if they don't exist
        // Using EnsureCreated instead of Migrate for simplicity
        // BUGFIX: ConfigureAwait(false) prevents deadlock when called from UI thread with .GetAwaiter().GetResult()
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);

        // Apply GitOps schema migration (adds PublicId columns if needed)
        await context.EnsureGitOpsSchemaMigrationAsync().ConfigureAwait(false);

        // Seed initial data
        var seedService = scope.ServiceProvider.GetRequiredService<ISeedService>();
        await seedService.SeedAsync().ConfigureAwait(false);

        // Cleanup old history entries using user's configured retention days
        // BUGFIX: Load settings before accessing CurrentSettings property
        var settingsService = _serviceProvider!.GetRequiredService<ISettingsService>();
        await settingsService.LoadSettingsAsync().ConfigureAwait(false);
        var historyService = scope.ServiceProvider.GetRequiredService<ICommandHistoryService>();
        await historyService.CleanupOldEntriesAsync(settingsService.CurrentSettings.AutoCleanupDays).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs automatic Git synchronization at startup if enabled in settings.
    /// This runs asynchronously after the main window is shown to not block the UI.
    /// </summary>
    private async Task PerformStartupGitSyncAsync()
    {
        if (_serviceProvider == null) return;

        try
        {
            var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
            var settings = settingsService.CurrentSettings;

            // Check if auto-sync is enabled and Git is configured
            if (!settings.GitSyncOnStartup ||
                string.IsNullOrWhiteSpace(settings.GitRemoteUrl) ||
                string.IsNullOrWhiteSpace(settings.GitRepositoryPath))
            {
                _logger.LogInfo("Git auto-sync disabled or not configured, skipping startup sync");
                return;
            }

            _logger.LogInfo("Starting automatic Git synchronization...");

            var gitSyncService = _serviceProvider.GetRequiredService<IGitSyncService>();

            // Perform full sync (pull + import, and optionally export + push)
            var result = await gitSyncService.FullSyncAsync();

            if (result.Success)
            {
                _logger.LogInfo($"Git sync completed: {result.ItemsImported} imported, {result.ItemsExported} exported");

                // Show notification to user
                var notificationService = _serviceProvider.GetRequiredService<INotificationService>();
                notificationService.ShowSuccess(
                    $"Sync complete: {result.ItemsImported} imported, {result.ItemsExported} exported",
                    "Git Sync");
            }
            else
            {
                _logger.LogError($"Git sync failed: {result.Message} - {result.ErrorDetails}");

                // Show error notification (non-blocking)
                var notificationService = _serviceProvider.GetRequiredService<INotificationService>();
                notificationService.ShowError($"Sync failed: {result.Message}", "Git Sync Error");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during startup Git sync", ex);
            // Don't show error to user for startup sync failures - they can check settings
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger.LogInfo("Application exiting...");
        _logger.Dispose(); // Flush all pending logs
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
