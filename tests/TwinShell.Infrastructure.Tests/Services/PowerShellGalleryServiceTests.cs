using FluentAssertions;
using Moq;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Infrastructure.Services;
using Xunit;

namespace TwinShell.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for PowerShellGalleryService
/// Note: These tests use mocking where appropriate to avoid actual network calls
/// </summary>
public class PowerShellGalleryServiceTests
{
    private readonly Mock<ICommandExecutionService> _mockCommandExecutionService;
    private readonly PowerShellGalleryService _sut;

    public PowerShellGalleryServiceTests()
    {
        _mockCommandExecutionService = new Mock<ICommandExecutionService>();
        _sut = new PowerShellGalleryService(_mockCommandExecutionService.Object);
    }

    [Fact]
    public async Task SearchModulesAsync_WithValidQuery_ReturnsModules()
    {
        // Arrange
        var query = "PSReadLine";
        var jsonResponse = @"[
            {
                ""Name"": ""PSReadLine"",
                ""Version"": ""2.2.6"",
                ""Description"": ""PowerShell module for improved console experience"",
                ""Author"": ""Microsoft"",
                ""CompanyName"": ""Microsoft Corporation"",
                ""PublishedDate"": ""2023-01-15T00:00:00"",
                ""DownloadCount"": 1000000,
                ""Tags"": ""console readline""
            }
        ]";

        _mockCommandExecutionService
            .Setup(x => x.ExecuteAsync(
                It.Is<string>(cmd => cmd.Contains("Find-Module") && cmd.Contains(query)),
                Platform.Windows,
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                null))
            .ReturnsAsync(new Core.Models.CommandResult
            {
                Success = true,
                ExitCode = 0,
                Stdout = jsonResponse,
                Stderr = string.Empty,
                Duration = TimeSpan.FromSeconds(2)
            });

        // Act
        var result = await _sut.SearchModulesAsync(query, maxResults: 10, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("PSReadLine");
        result[0].Version.Should().Be("2.2.6");
        result[0].Author.Should().Be("Microsoft");
    }

    [Fact]
    public async Task SearchModulesAsync_WithEmptyQuery_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _sut.SearchModulesAsync(string.Empty, maxResults: 10, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("query");
    }

    [Fact]
    public async Task SearchModulesAsync_NoResults_ReturnsEmptyList()
    {
        // Arrange
        var query = "NonExistentModule12345";
        var jsonResponse = "[]";

        _mockCommandExecutionService
            .Setup(x => x.ExecuteAsync(
                It.Is<string>(cmd => cmd.Contains("Find-Module") && cmd.Contains(query)),
                Platform.Windows,
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                null))
            .ReturnsAsync(new Core.Models.CommandResult
            {
                Success = true,
                ExitCode = 0,
                Stdout = jsonResponse,
                Stderr = string.Empty,
                Duration = TimeSpan.FromSeconds(1)
            });

        // Act
        var result = await _sut.SearchModulesAsync(query, maxResults: 10, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchModulesAsync_CommandFails_ThrowsException()
    {
        // Arrange
        var query = "TestModule";

        _mockCommandExecutionService
            .Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                Platform.Windows,
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                null))
            .ReturnsAsync(new Core.Models.CommandResult
            {
                Success = false,
                ExitCode = 1,
                Stdout = string.Empty,
                Stderr = "Network error",
                ErrorMessage = "Failed to connect to PowerShell Gallery",
                Duration = TimeSpan.FromSeconds(1)
            });

        // Act
        Func<Task> act = async () => await _sut.SearchModulesAsync(query, maxResults: 10, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to search PowerShell Gallery*");
    }

    [Fact]
    public async Task GetModuleDetailsAsync_WithValidModuleName_ReturnsDetails()
    {
        // Arrange
        var moduleName = "PSReadLine";
        var jsonResponse = @"{
            ""Name"": ""PSReadLine"",
            ""Version"": ""2.2.6"",
            ""Description"": ""PowerShell module for improved console experience"",
            ""Author"": ""Microsoft"",
            ""CompanyName"": ""Microsoft Corporation"",
            ""PublishedDate"": ""2023-01-15T00:00:00"",
            ""DownloadCount"": 1000000,
            ""Tags"": ""console readline"",
            ""Dependencies"": """",
            ""LicenseUri"": ""https://github.com/PowerShell/PSReadLine/blob/master/LICENSE"",
            ""ProjectUri"": ""https://github.com/PowerShell/PSReadLine""
        }";

        _mockCommandExecutionService
            .Setup(x => x.ExecuteAsync(
                It.Is<string>(cmd => cmd.Contains("Find-Module") && cmd.Contains(moduleName) && cmd.Contains("-AllVersions") == false),
                Platform.Windows,
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                null))
            .ReturnsAsync(new Core.Models.CommandResult
            {
                Success = true,
                ExitCode = 0,
                Stdout = jsonResponse,
                Stderr = string.Empty,
                Duration = TimeSpan.FromSeconds(2)
            });

        // Act
        var result = await _sut.GetModuleDetailsAsync(moduleName, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("PSReadLine");
        result.Version.Should().Be("2.2.6");
        result.Description.Should().Contain("PowerShell module");
    }

    [Fact]
    public async Task GetModuleDetailsAsync_ModuleNotFound_ThrowsException()
    {
        // Arrange
        var moduleName = "NonExistentModule12345";

        _mockCommandExecutionService
            .Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                Platform.Windows,
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                null))
            .ReturnsAsync(new Core.Models.CommandResult
            {
                Success = false,
                ExitCode = 1,
                Stdout = string.Empty,
                Stderr = "Module not found",
                ErrorMessage = "No match was found for the specified search criteria",
                Duration = TimeSpan.FromSeconds(1)
            });

        // Act
        Func<Task> act = async () => await _sut.GetModuleDetailsAsync(moduleName, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task InstallModuleAsync_WithValidModule_ReturnsSuccess()
    {
        // Arrange
        var moduleName = "PSReadLine";
        var version = "2.2.6";

        _mockCommandExecutionService
            .Setup(x => x.ExecuteAsync(
                It.Is<string>(cmd => cmd.Contains("Install-Module") && cmd.Contains(moduleName) && cmd.Contains(version)),
                Platform.Windows,
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                null))
            .ReturnsAsync(new Core.Models.CommandResult
            {
                Success = true,
                ExitCode = 0,
                Stdout = "Module installed successfully",
                Stderr = string.Empty,
                Duration = TimeSpan.FromSeconds(10)
            });

        // Act
        var result = await _sut.InstallModuleAsync(moduleName, version, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task InstallModuleAsync_WithInvalidModule_ReturnsFailure()
    {
        // Arrange
        var moduleName = "NonExistentModule12345";
        var version = "1.0.0";

        _mockCommandExecutionService
            .Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                Platform.Windows,
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                null))
            .ReturnsAsync(new Core.Models.CommandResult
            {
                Success = false,
                ExitCode = 1,
                Stdout = string.Empty,
                Stderr = "Module not found in repository",
                ErrorMessage = "Installation failed",
                Duration = TimeSpan.FromSeconds(2)
            });

        // Act
        var result = await _sut.InstallModuleAsync(moduleName, version, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("failed");
    }

    [Fact]
    public async Task SearchModulesAsync_RateLimiting_HandlesGracefully()
    {
        // Arrange
        var query = "TestModule";

        _mockCommandExecutionService
            .Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                Platform.Windows,
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                null))
            .ReturnsAsync(new Core.Models.CommandResult
            {
                Success = false,
                ExitCode = 1,
                Stdout = string.Empty,
                Stderr = "429 Too Many Requests",
                ErrorMessage = "Rate limit exceeded",
                Duration = TimeSpan.FromSeconds(1)
            });

        // Act
        Func<Task> act = async () => await _sut.SearchModulesAsync(query, maxResults: 10, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to search PowerShell Gallery*");
    }

    [Fact]
    public async Task SearchModulesAsync_WithCancellation_PropagatesCancellation()
    {
        // Arrange
        var query = "TestModule";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockCommandExecutionService
            .Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                Platform.Windows,
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                null))
            .ReturnsAsync(new Core.Models.CommandResult
            {
                Success = false,
                ExitCode = -1,
                Stdout = string.Empty,
                Stderr = string.Empty,
                ErrorMessage = "Operation cancelled",
                WasCancelled = true,
                Duration = TimeSpan.Zero
            });

        // Act
        Func<Task> act = async () => await _sut.SearchModulesAsync(query, maxResults: 10, cts.Token);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SearchModulesAsync_InvalidJson_HandlesGracefully()
    {
        // Arrange
        var query = "TestModule";
        var invalidJson = "{ this is not valid json }";

        _mockCommandExecutionService
            .Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                Platform.Windows,
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                null))
            .ReturnsAsync(new Core.Models.CommandResult
            {
                Success = true,
                ExitCode = 0,
                Stdout = invalidJson,
                Stderr = string.Empty,
                Duration = TimeSpan.FromSeconds(1)
            });

        // Act
        var result = await _sut.SearchModulesAsync(query, maxResults: 10, CancellationToken.None);

        // Assert
        // Should return empty list or handle gracefully rather than throwing
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
