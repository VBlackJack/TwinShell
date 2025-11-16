using FluentAssertions;
using TwinShell.Core.Models;
using TwinShell.Infrastructure.Services;

namespace TwinShell.Infrastructure.Tests.Services;

public class PackageManagerServiceTests
{
    private readonly PackageManagerService _service;

    public PackageManagerServiceTests()
    {
        _service = new PackageManagerService();
    }

    [Fact]
    public async Task SearchWingetPackagesAsync_WithEmptySearchTerm_ReturnsEmptyList()
    {
        // Act
        var result = await _service.SearchWingetPackagesAsync("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchWingetPackagesAsync_WithNullSearchTerm_ReturnsEmptyList()
    {
        // Act
        var result = await _service.SearchWingetPackagesAsync(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchChocolateyPackagesAsync_WithEmptySearchTerm_ReturnsEmptyList()
    {
        // Act
        var result = await _service.SearchChocolateyPackagesAsync("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchChocolateyPackagesAsync_WithNullSearchTerm_ReturnsEmptyList()
    {
        // Act
        var result = await _service.SearchChocolateyPackagesAsync(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWingetPackageInfoAsync_WithEmptyPackageId_ReturnsNull()
    {
        // Act
        var result = await _service.GetWingetPackageInfoAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWingetPackageInfoAsync_WithNullPackageId_ReturnsNull()
    {
        // Act
        var result = await _service.GetWingetPackageInfoAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetChocolateyPackageInfoAsync_WithEmptyPackageId_ReturnsNull()
    {
        // Act
        var result = await _service.GetChocolateyPackageInfoAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetChocolateyPackageInfoAsync_WithNullPackageId_ReturnsNull()
    {
        // Act
        var result = await _service.GetChocolateyPackageInfoAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task IsWingetAvailableAsync_ReturnsBoolean()
    {
        // Act
        var result = await _service.IsWingetAvailableAsync();

        // Assert
        result.Should().BeOfType<bool>();
    }

    [Fact]
    public async Task IsChocolateyAvailableAsync_ReturnsBoolean()
    {
        // Act
        var result = await _service.IsChocolateyAvailableAsync();

        // Assert
        result.Should().BeOfType<bool>();
    }

    [Fact]
    public async Task ListWingetInstalledPackagesAsync_ReturnsEnumerable()
    {
        // Act
        var result = await _service.ListWingetInstalledPackagesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IEnumerable<PackageSearchResult>>();
    }

    [Fact]
    public async Task ListChocolateyInstalledPackagesAsync_ReturnsEnumerable()
    {
        // Act
        var result = await _service.ListChocolateyInstalledPackagesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IEnumerable<PackageSearchResult>>();
    }

    // Additional integration tests (may require actual Winget/Chocolatey installation)
    // These are commented out as they require system dependencies

    /*
    [Fact]
    [Trait("Category", "Integration")]
    public async Task SearchWingetPackagesAsync_WithValidSearchTerm_ReturnsResults()
    {
        // Arrange
        var searchTerm = "powertoys";

        // Act
        var result = await _service.SearchWingetPackagesAsync(searchTerm);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(p => p.PackageManager == PackageManager.Winget);
        result.Should().Contain(p => !string.IsNullOrEmpty(p.Name));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetWingetPackageInfoAsync_WithValidPackageId_ReturnsPackageInfo()
    {
        // Arrange
        var packageId = "Microsoft.PowerToys";

        // Act
        var result = await _service.GetWingetPackageInfoAsync(packageId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(packageId);
        result.PackageManager.Should().Be(PackageManager.Winget);
        result.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SearchChocolateyPackagesAsync_WithValidSearchTerm_ReturnsResults()
    {
        // Arrange
        var searchTerm = "googlechrome";

        // Act
        var result = await _service.SearchChocolateyPackagesAsync(searchTerm);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(p => p.PackageManager == PackageManager.Chocolatey);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetChocolateyPackageInfoAsync_WithValidPackageId_ReturnsPackageInfo()
    {
        // Arrange
        var packageId = "googlechrome";

        // Act
        var result = await _service.GetChocolateyPackageInfoAsync(packageId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(packageId);
        result.PackageManager.Should().Be(PackageManager.Chocolatey);
    }
    */
}
