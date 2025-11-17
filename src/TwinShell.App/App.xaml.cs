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

        // Add global exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        try
        {
            LogInfo("Starting application...");

            var services = new ServiceCollection();
            LogInfo("Configuring services...");
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            LogInfo("Services configured");

            // Initialize theme and database
            // BUGFIX: Skip async theme initialization during startup - will be done after window is shown
            //LogInfo("Initializing theme...");
            //InitializeThemeAsync().GetAwaiter().GetResult();
            //LogInfo("Theme initialized");

            LogInfo("Initializing database...");
            InitializeDatabaseAsync().GetAwaiter().GetResult();
            LogInfo("Database initialized");

            // Create and show main window
            LogInfo("Creating main window...");
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            LogInfo("Main window created");

            LogInfo("Showing main window...");
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Show();
            mainWindow.Activate();
            mainWindow.Topmost = true;
            mainWindow.Topmost = false; // Set to true then false to bring to front
            LogInfo("Main window shown!");
        }
        catch (Exception ex)
        {
            LogError("Startup error", ex);
            // SECURITY: Don't expose detailed error messages to users
            MessageBox.Show("Une erreur s'est produite au démarrage de l'application.\n\nVeuillez consulter le fichier startup-error.log pour plus de détails.",
                "Erreur de démarrage", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogError("Unhandled exception", ex);
        }
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogError("Dispatcher exception", e.Exception);
        // SECURITY: Don't expose detailed error messages to users
        MessageBox.Show("Une erreur inattendue s'est produite.\n\nL'application va continuer à fonctionner mais certaines fonctionnalités peuvent être affectées.",
            "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void LogError(string message, Exception ex)
    {
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup-error.log");
            // SECURITY: Sanitize exception logging to avoid exposing sensitive paths or data
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message}\n" +
                           $"Type: {ex.GetType().Name}\n" +
                           $"Message: {ex.Message}\n" +
                           $"Source: {ex.Source}\n\n";
            File.AppendAllText(logPath, logMessage);
        }
        catch { /* Ignore logging errors */ }
    }

    private void LogInfo(string message)
    {
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log");
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
            File.AppendAllText(logPath, logMessage);
        }
        catch { /* Ignore logging errors */ }
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
        services.AddSingleton<IDialogService, TwinShell.App.Services.DialogService>();
        services.AddScoped<ICommandExecutionService, CommandExecutionService>();

        // Seed Service
        var seedFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "seed", "initial-actions.json");
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

        // Views
        services.AddTransient<HistoryPanel>();
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
        // BUGFIX: Added null-check to prevent NullReferenceException if OnStartup fails before _serviceProvider is initialized
        if (_serviceProvider == null)
        {
            throw new InvalidOperationException("Service provider has not been initialized");
        }

        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var themeService = _serviceProvider.GetRequiredService<IThemeService>();
        var localizationService = _serviceProvider.GetRequiredService<ILocalizationService>();

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
        // BUGFIX: Added null-check to prevent NullReferenceException if OnStartup fails before _serviceProvider is initialized
        if (_serviceProvider == null)
        {
            throw new InvalidOperationException("Service provider has not been initialized");
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TwinShellDbContext>();

        // Create database and tables if they don't exist
        // Using EnsureCreated instead of Migrate for simplicity
        await context.Database.EnsureCreatedAsync();

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
