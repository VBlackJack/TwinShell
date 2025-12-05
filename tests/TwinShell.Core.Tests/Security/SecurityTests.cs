using System.Net;
using Microsoft.Extensions.Logging;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using TwinShell.Core.Services;
using Xunit;
using Moq;

namespace TwinShell.Core.Tests.Security;

/// <summary>
/// Security tests to validate vulnerability fixes for TwinShell
/// Tests for: Command Injection, Path Traversal, Input Validation, etc.
/// </summary>
public class SecurityTests
{
    #region Command Injection Tests

    [Fact]
    public void GenerateCommand_RejectsCommandInjectionAttempt_WithAmpersand()
    {
        // Arrange
        var service = new CommandGeneratorService(new Mock<ILocalizationService>().Object);
        var template = new CommandTemplate
        {
            CommandPattern = "Get-ChildItem {path}",
            Parameters = new List<TemplateParameter>
            {
                new() { Name = "path", Type = "string", Required = true }
            }
        };
        var parameters = new Dictionary<string, string>
        {
            { "path", "C:\\temp && Del C:\\Windows" }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            service.GenerateCommand(template, parameters));
    }

    [Fact]
    public void GenerateCommand_RejectsCommandInjectionAttempt_WithPipe()
    {
        // Arrange
        var service = new CommandGeneratorService(new Mock<ILocalizationService>().Object);
        var template = new CommandTemplate
        {
            CommandPattern = "Get-Process {name}",
            Parameters = new List<TemplateParameter>
            {
                new() { Name = "name", Type = "string", Required = true }
            }
        };
        var parameters = new Dictionary<string, string>
        {
            { "name", "notepad | rm -rf /" }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            service.GenerateCommand(template, parameters));
    }

    [Fact]
    public void GenerateCommand_RejectsCommandInjectionAttempt_WithSemicolon()
    {
        // Arrange
        var service = new CommandGeneratorService(new Mock<ILocalizationService>().Object);
        var template = new CommandTemplate
        {
            CommandPattern = "ping {host}",
            Parameters = new List<TemplateParameter>
            {
                new() { Name = "host", Type = "string", Required = true }
            }
        };
        var parameters = new Dictionary<string, string>
        {
            { "host", "127.0.0.1; cat /etc/passwd" }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            service.GenerateCommand(template, parameters));
    }

    [Fact]
    public void GenerateCommand_RejectsCommandInjectionAttempt_WithBacktick()
    {
        // Arrange
        var service = new CommandGeneratorService(new Mock<ILocalizationService>().Object);
        var template = new CommandTemplate
        {
            CommandPattern = "echo {message}",
            Parameters = new List<TemplateParameter>
            {
                new() { Name = "message", Type = "string", Required = true }
            }
        };
        var parameters = new Dictionary<string, string>
        {
            { "message", "`whoami`" }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            service.GenerateCommand(template, parameters));
    }

    [Fact]
    public void GenerateCommand_RejectsCommandInjectionAttempt_WithDollarSign()
    {
        // Arrange
        var service = new CommandGeneratorService(new Mock<ILocalizationService>().Object);
        var template = new CommandTemplate
        {
            CommandPattern = "Write-Host {text}",
            Parameters = new List<TemplateParameter>
            {
                new() { Name = "text", Type = "string", Required = true }
            }
        };
        var parameters = new Dictionary<string, string>
        {
            { "text", "$(Get-Process)" }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            service.GenerateCommand(template, parameters));
    }

    #endregion

    #region Input Validation Tests

    [Fact]
    public void ValidateParameters_RejectsTooLongString()
    {
        // Arrange
        var mockLocalizationService = new Mock<ILocalizationService>();
        mockLocalizationService
            .Setup(x => x.GetFormattedString(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] args) => $"Le paramètre {args[0]} dépasse la longueur maximale autorisée");
        var service = new CommandGeneratorService(mockLocalizationService.Object);
        var template = new CommandTemplate
        {
            Parameters = new List<TemplateParameter>
            {
                new() { Name = "message", Type = "string", Required = true, Label = "Message" }
            }
        };
        var parameters = new Dictionary<string, string>
        {
            { "message", new string('A', 300) } // 300 characters (max is 255)
        };

        // Act
        var isValid = service.ValidateParameters(template, parameters, out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e != null && e.Contains("dépasse la longueur maximale"));
    }

    [Fact]
    public void ValidateParameters_RejectsInvalidHostname()
    {
        // Arrange
        var mockLocalizationService = new Mock<ILocalizationService>();
        mockLocalizationService
            .Setup(x => x.GetFormattedString(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] args) => $"Le paramètre {args[0]} doit être un nom d'hôte valide");
        var service = new CommandGeneratorService(mockLocalizationService.Object);
        var template = new CommandTemplate
        {
            Parameters = new List<TemplateParameter>
            {
                new() { Name = "host", Type = "hostname", Required = true, Label = "Hostname" }
            }
        };
        var parameters = new Dictionary<string, string>
        {
            { "host", "invalid..hostname" }
        };

        // Act
        var isValid = service.ValidateParameters(template, parameters, out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e != null && e.Contains("nom d'hôte valide"));
    }

    [Fact]
    public void ValidateParameters_AcceptsValidHostname()
    {
        // Arrange
        var service = new CommandGeneratorService(new Mock<ILocalizationService>().Object);
        var template = new CommandTemplate
        {
            Parameters = new List<TemplateParameter>
            {
                new() { Name = "host", Type = "hostname", Required = true, Label = "Hostname" }
            }
        };
        var parameters = new Dictionary<string, string>
        {
            { "host", "example.com" }
        };

        // Act
        var isValid = service.ValidateParameters(template, parameters, out var errors);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateParameters_RejectsInvalidIPAddress()
    {
        // Arrange
        var mockLocalizationService = new Mock<ILocalizationService>();
        mockLocalizationService
            .Setup(x => x.GetFormattedString(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] args) => $"Le paramètre {args[0]} doit être une adresse IP valide");
        var service = new CommandGeneratorService(mockLocalizationService.Object);
        var template = new CommandTemplate
        {
            Parameters = new List<TemplateParameter>
            {
                new() { Name = "ip", Type = "ipaddress", Required = true, Label = "IP Address" }
            }
        };
        var parameters = new Dictionary<string, string>
        {
            { "ip", "999.999.999.999" }
        };

        // Act
        var isValid = service.ValidateParameters(template, parameters, out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e != null && e.Contains("adresse IP valide"));
    }

    [Fact]
    public void ValidateParameters_AcceptsValidIPAddress()
    {
        // Arrange
        var service = new CommandGeneratorService(new Mock<ILocalizationService>().Object);
        var template = new CommandTemplate
        {
            Parameters = new List<TemplateParameter>
            {
                new() { Name = "ip", Type = "ipaddress", Required = true, Label = "IP Address" }
            }
        };
        var parameters = new Dictionary<string, string>
        {
            { "ip", "192.168.1.1" }
        };

        // Act
        var isValid = service.ValidateParameters(template, parameters, out var errors);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    #endregion

    #region Path Traversal Tests

    [Fact]
    public async Task ExportToJsonAsync_RejectsPathTraversal_WithDoubleDots()
    {
        // Arrange
        var service = CreateConfigurationService();
        var maliciousPath = "../../../../etc/passwd";

        // Act
        var result = await service.ExportToJsonAsync(maliciousPath);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid file path", result.ErrorMessage);
    }

    [Fact]
    public async Task ExportToJsonAsync_RejectsPathTraversal_WithTilde()
    {
        // Arrange
        var service = CreateConfigurationService();
        var maliciousPath = "~/../../etc/passwd";

        // Act
        var result = await service.ExportToJsonAsync(maliciousPath);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid file path", result.ErrorMessage);
    }

    [Fact]
    public async Task ExportToJsonAsync_RejectsNonJsonExtension()
    {
        // Arrange
        var service = CreateConfigurationService();
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var filePath = Path.Combine(appData, "TwinShell", "Exports", "test.txt");

        // Act
        var result = await service.ExportToJsonAsync(filePath);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("must be a .json file", result.ErrorMessage);
    }

    [Fact]
    public async Task ImportFromJsonAsync_RejectsPathTraversal()
    {
        // Arrange
        var service = CreateConfigurationService();
        var maliciousPath = "../../../../etc/passwd";

        // Act
        var result = await service.ImportFromJsonAsync(maliciousPath);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid file path", result.ErrorMessage);
    }

    #endregion

    #region String Length Validation Tests

    [Fact]
    public void ValidateParameters_RejectsExcessivelyLongString()
    {
        // Arrange
        var service = new CommandGeneratorService(new Mock<ILocalizationService>().Object);
        var template = new CommandTemplate
        {
            Parameters = new List<TemplateParameter>
            {
                new() { Name = "text", Type = "string", Required = true, Label = "Text" }
            }
        };
        var parameters = new Dictionary<string, string>
        {
            { "text", new string('X', 500) }
        };

        // Act
        var isValid = service.ValidateParameters(template, parameters, out var errors);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(errors);
    }

    #endregion

    #region Dangerous Characters Tests

    [Theory]
    [InlineData("test\nvalue")]  // Newline
    [InlineData("test\rvalue")]  // Carriage return
    [InlineData("test<value")]   // Less than
    [InlineData("test>value")]   // Greater than
    [InlineData("test(value")]   // Parenthesis
    [InlineData("test)value")]   // Parenthesis
    public void ValidateParameters_RejectsDangerousCharacters(string maliciousValue)
    {
        // Arrange
        var mockLocalizationService = new Mock<ILocalizationService>();
        // Configure mock to return a message containing "caractères interdits"
        mockLocalizationService
            .Setup(x => x.GetFormattedString(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] args) => $"Le paramètre {args[0]} contient des caractères interdits");

        var service = new CommandGeneratorService(mockLocalizationService.Object);
        var template = new CommandTemplate
        {
            Parameters = new List<TemplateParameter>
            {
                new() { Name = "input", Type = "string", Required = true, Label = "Input" }
            }
        };
        var parameters = new Dictionary<string, string>
        {
            { "input", maliciousValue }
        };

        // Act
        var isValid = service.ValidateParameters(template, parameters, out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e != null && e.Contains("caractères interdits"));
    }

    #endregion

    #region Settings Encryption Tests

    [Fact]
    public async Task SaveSettingsAsync_EncryptsData()
    {
        // Arrange - Use isolated temp directory to avoid test interference
        var tempDir = Path.Combine(Path.GetTempPath(), $"TwinShell_Test_{Guid.NewGuid()}");
        try
        {
            var service = new SettingsService(tempDir);
            var settings = UserSettings.Default;

            // Act
            var result = await service.SaveSettingsAsync(settings);

            // Assert
            Assert.True(result);

            // Verify file exists
            var settingsPath = service.GetSettingsFilePath();
            Assert.True(File.Exists(settingsPath));

            // Read raw file contents - should be encrypted (not plain JSON)
            var rawBytes = await File.ReadAllBytesAsync(settingsPath);
            var rawText = System.Text.Encoding.UTF8.GetString(rawBytes);

            // Encrypted data should not contain readable JSON structure
            Assert.DoesNotContain("\"AutoCleanupDays\"", rawText);
            Assert.DoesNotContain("\"MaxHistoryItems\"", rawText);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task LoadSettingsAsync_DecryptsData()
    {
        // Arrange - Use isolated temp directory to avoid test interference
        var tempDir = Path.Combine(Path.GetTempPath(), $"TwinShell_Test_{Guid.NewGuid()}");
        try
        {
            var service = new SettingsService(tempDir);
            var settings = new UserSettings
            {
                AutoCleanupDays = 100,
                MaxHistoryItems = 5000
            };

            // Act - Save encrypted
            await service.SaveSettingsAsync(settings);

            // Act - Load decrypted
            var loadedSettings = await service.LoadSettingsAsync();

            // Assert
            Assert.Equal(settings.AutoCleanupDays, loadedSettings.AutoCleanupDays);
            Assert.Equal(settings.MaxHistoryItems, loadedSettings.MaxHistoryItems);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    #endregion

    #region Helper Methods

    private ConfigurationService CreateConfigurationService()
    {
        var mockFavoritesRepo = new Mock<IFavoritesRepository>();
        var historyRepo = new FakeCommandHistoryRepository();
        var actionRepo = new FakeActionRepository();
        var mockLogger = new Mock<ILogger<ConfigurationService>>();

        return new ConfigurationService(mockFavoritesRepo.Object, historyRepo, actionRepo, mockLogger.Object);
    }

    // Fake repository for UserFavorite testing (simplified stub)
    private class FakeRepository<T> where T : class
    {
        public Task<T?> GetByIdAsync(string id) => Task.FromResult<T?>(null);
        public Task<IEnumerable<T>> GetAllAsync(string? userId = null) => Task.FromResult(Enumerable.Empty<T>());
        public Task AddAsync(T entity) => Task.CompletedTask;
        public Task UpdateAsync(T entity) => Task.CompletedTask;
        public Task DeleteAsync(string id) => Task.CompletedTask;
        public Task<int> GetCountAsync(string? userId = null) => Task.FromResult(0);
    }

    private class FakeCommandHistoryRepository : ICommandHistoryRepository
    {
        public Task<CommandHistory?> GetByIdAsync(string id) => Task.FromResult<CommandHistory?>(null);
        public Task AddAsync(CommandHistory entity) => Task.CompletedTask;
        public Task AddRangeAsync(IEnumerable<CommandHistory> histories) => Task.CompletedTask;
        public Task UpdateAsync(CommandHistory entity) => Task.CompletedTask;
        public Task DeleteAsync(string id) => Task.CompletedTask;
        public Task DeleteRangeAsync(IEnumerable<string> ids) => Task.CompletedTask;
        public Task DeleteOlderThanAsync(DateTime date) => Task.CompletedTask;
        public Task<IEnumerable<CommandHistory>> GetRecentAsync(int count = 50) => Task.FromResult(Enumerable.Empty<CommandHistory>());
        public Task<IEnumerable<CommandHistory>> SearchAsync(string? searchText = null, DateTime? fromDate = null, DateTime? toDate = null, Platform? platform = null, string? category = null) => Task.FromResult(Enumerable.Empty<CommandHistory>());
        public Task<int> CountAsync() => Task.FromResult(0);
        public Task ClearAllAsync() => Task.CompletedTask;
    }

    private class FakeActionRepository : IActionRepository
    {
        public Task<TwinShell.Core.Models.Action?> GetByIdAsync(string id) => Task.FromResult<TwinShell.Core.Models.Action?>(null);
        public Task<IEnumerable<TwinShell.Core.Models.Action>> GetAllAsync() => Task.FromResult(Enumerable.Empty<TwinShell.Core.Models.Action>());
        public Task AddAsync(TwinShell.Core.Models.Action entity) => Task.CompletedTask;
        public Task UpdateAsync(TwinShell.Core.Models.Action entity) => Task.CompletedTask;
        public Task DeleteAsync(string id) => Task.CompletedTask;
        public Task<IEnumerable<TwinShell.Core.Models.Action>> SearchAsync(string query) => Task.FromResult(Enumerable.Empty<TwinShell.Core.Models.Action>());
        public Task<IEnumerable<TwinShell.Core.Models.Action>> GetByCategoryAsync(string category) => Task.FromResult(Enumerable.Empty<TwinShell.Core.Models.Action>());
        public Task<IEnumerable<string>> GetAllCategoriesAsync() => Task.FromResult(Enumerable.Empty<string>());
        public Task<int> CountAsync() => Task.FromResult(0);
        public Task<bool> ExistsAsync(string id) => Task.FromResult(false);
        public Task<int> UpdateCategoryForActionsAsync(string oldCategory, string? newCategory) => Task.FromResult(0);
    }

    #endregion
}
