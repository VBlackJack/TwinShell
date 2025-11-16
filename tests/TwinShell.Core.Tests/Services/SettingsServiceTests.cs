using FluentAssertions;
using TwinShell.Core.Enums;
using TwinShell.Core.Models;
using TwinShell.Core.Services;

namespace TwinShell.Core.Tests.Services;

public class SettingsServiceTests : IDisposable
{
    private readonly SettingsService _service;
    private readonly string _testSettingsPath;

    public SettingsServiceTests()
    {
        _service = new SettingsService();
        _testSettingsPath = _service.GetSettingsFilePath();
    }

    public void Dispose()
    {
        // Clean up test settings file
        if (File.Exists(_testSettingsPath))
        {
            File.Delete(_testSettingsPath);
        }
    }

    [Fact]
    public async Task LoadSettingsAsync_ReturnsDefaultSettings_WhenFileDoesNotExist()
    {
        // Arrange
        if (File.Exists(_testSettingsPath))
        {
            File.Delete(_testSettingsPath);
        }

        // Act
        var settings = await _service.LoadSettingsAsync();

        // Assert
        settings.Should().NotBeNull();
        settings.Theme.Should().Be(Theme.Light);
        settings.AutoCleanupDays.Should().Be(90);
        settings.MaxHistoryItems.Should().Be(1000);
        settings.RecentCommandsCount.Should().Be(5);
        settings.ShowRecentCommandsWidget.Should().BeTrue();
        settings.ConfirmDangerousActions.Should().BeTrue();
    }

    [Fact]
    public async Task SaveSettingsAsync_CreatesFileWithCorrectData()
    {
        // Arrange
        var settings = new UserSettings
        {
            Theme = Theme.Dark,
            AutoCleanupDays = 30,
            MaxHistoryItems = 500,
            RecentCommandsCount = 10,
            ShowRecentCommandsWidget = false,
            ConfirmDangerousActions = false
        };

        // Act
        var result = await _service.SaveSettingsAsync(settings);

        // Assert
        result.Should().BeTrue();
        File.Exists(_testSettingsPath).Should().BeTrue();

        // Verify content
        var loadedSettings = await _service.LoadSettingsAsync();
        loadedSettings.Theme.Should().Be(Theme.Dark);
        loadedSettings.AutoCleanupDays.Should().Be(30);
        loadedSettings.MaxHistoryItems.Should().Be(500);
        loadedSettings.RecentCommandsCount.Should().Be(10);
        loadedSettings.ShowRecentCommandsWidget.Should().BeFalse();
        loadedSettings.ConfirmDangerousActions.Should().BeFalse();
    }

    [Fact]
    public async Task SaveSettingsAsync_UpdatesCurrentSettings()
    {
        // Arrange
        var settings = new UserSettings
        {
            Theme = Theme.Dark,
            AutoCleanupDays = 45
        };

        // Act
        await _service.SaveSettingsAsync(settings);

        // Assert
        _service.CurrentSettings.Theme.Should().Be(Theme.Dark);
        _service.CurrentSettings.AutoCleanupDays.Should().Be(45);
    }

    [Fact]
    public async Task ResetToDefaultAsync_RestoresDefaultSettings()
    {
        // Arrange
        var customSettings = new UserSettings
        {
            Theme = Theme.Dark,
            AutoCleanupDays = 30
        };
        await _service.SaveSettingsAsync(customSettings);

        // Act
        var resetSettings = await _service.ResetToDefaultAsync();

        // Assert
        resetSettings.Theme.Should().Be(Theme.Light);
        resetSettings.AutoCleanupDays.Should().Be(90);
        _service.CurrentSettings.Theme.Should().Be(Theme.Light);
    }

    [Fact]
    public void ValidateSettings_ReturnsTrue_ForValidSettings()
    {
        // Arrange
        var settings = new UserSettings
        {
            AutoCleanupDays = 30,
            MaxHistoryItems = 1000,
            RecentCommandsCount = 10
        };

        // Act
        var result = _service.ValidateSettings(settings);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateSettings_ReturnsFalse_WhenAutoCleanupDaysTooLow()
    {
        // Arrange
        var settings = new UserSettings
        {
            AutoCleanupDays = 0  // Below minimum of 1
        };

        // Act
        var result = _service.ValidateSettings(settings);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSettings_ReturnsFalse_WhenAutoCleanupDaysTooHigh()
    {
        // Arrange
        var settings = new UserSettings
        {
            AutoCleanupDays = 4000  // Above maximum of 3650
        };

        // Act
        var result = _service.ValidateSettings(settings);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSettings_ReturnsFalse_WhenMaxHistoryItemsTooLow()
    {
        // Arrange
        var settings = new UserSettings
        {
            MaxHistoryItems = 5  // Below minimum of 10
        };

        // Act
        var result = _service.ValidateSettings(settings);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSettings_ReturnsFalse_WhenRecentCommandsCountTooLow()
    {
        // Arrange
        var settings = new UserSettings
        {
            RecentCommandsCount = 0  // Below minimum of 1
        };

        // Act
        var result = _service.ValidateSettings(settings);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSettings_ReturnsFalse_WhenRecentCommandsCountTooHigh()
    {
        // Arrange
        var settings = new UserSettings
        {
            RecentCommandsCount = 100  // Above maximum of 50
        };

        // Act
        var result = _service.ValidateSettings(settings);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SaveSettingsAsync_ReturnsFalse_ForInvalidSettings()
    {
        // Arrange
        var invalidSettings = new UserSettings
        {
            AutoCleanupDays = -5  // Invalid
        };

        // Act
        var result = await _service.SaveSettingsAsync(invalidSettings);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetSettingsFilePath_ReturnsCorrectPath()
    {
        // Act
        var path = _service.GetSettingsFilePath();

        // Assert
        path.Should().NotBeNullOrEmpty();
        path.Should().Contain("TwinShell");
        path.Should().EndWith("settings.json");
    }
}
