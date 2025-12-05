using FluentAssertions;
using TwinShell.Core.Helpers;

namespace TwinShell.Core.Tests.Helpers;

/// <summary>
/// Tests for PathValidator security helper
/// </summary>
public class PathValidatorTests
{
    #region IsExportPathValid Tests

    [Fact]
    public void IsExportPathValid_WithValidPath_ReturnsTrue()
    {
        // Arrange
        var path = @"C:\Users\Test\Documents\export.json";

        // Act
        var result = PathValidator.IsExportPathValid(path);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExportPathValid_WithNullPath_ReturnsFalse()
    {
        // Act
        var result = PathValidator.IsExportPathValid(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsExportPathValid_WithEmptyPath_ReturnsFalse()
    {
        // Act
        var result = PathValidator.IsExportPathValid(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsExportPathValid_WithPathTraversal_ReturnsFalse()
    {
        // Arrange
        var path = @"C:\Users\Test\..\..\..\Windows\System32\config.json";

        // Act
        var result = PathValidator.IsExportPathValid(path);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsExportPathValid_WithUncPath_ReturnsFalse()
    {
        // Arrange
        var path = @"\\server\share\file.json";

        // Act
        var result = PathValidator.IsExportPathValid(path);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsExportPathValid_WithForwardSlashUncPath_ReturnsFalse()
    {
        // Arrange
        var path = "//server/share/file.json";

        // Act
        var result = PathValidator.IsExportPathValid(path);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsImportPathValid Tests

    [Fact]
    public void IsImportPathValid_WithValidJsonPath_ReturnsTrue()
    {
        // Arrange
        var path = @"C:\Users\Test\Documents\import.json";

        // Act
        var result = PathValidator.IsImportPathValid(path);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsImportPathValid_WithInvalidExtension_ReturnsFalse()
    {
        // Arrange
        var path = @"C:\Users\Test\Documents\import.txt";

        // Act
        var result = PathValidator.IsImportPathValid(path);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsImportPathValid_WithCustomExtension_ReturnsTrue()
    {
        // Arrange
        var path = @"C:\Users\Test\Documents\data.xml";

        // Act
        var result = PathValidator.IsImportPathValid(path, ".xml");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsImportPathValid_WithNullExtension_AcceptsAnyFile()
    {
        // Arrange
        var path = @"C:\Users\Test\Documents\anyfile.xyz";

        // Act
        var result = PathValidator.IsImportPathValid(path, null);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsImportPathValid_WithPathTraversal_ReturnsFalse()
    {
        // Arrange
        var path = @"C:\Users\Test\..\Admin\secrets.json";

        // Act
        var result = PathValidator.IsImportPathValid(path);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsImportPathValid_WithUncPath_ReturnsFalse()
    {
        // Arrange
        var path = @"\\malicious-server\share\payload.json";

        // Act
        var result = PathValidator.IsImportPathValid(path);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsPathWithinDirectory Tests

    [Fact]
    public void IsPathWithinDirectory_WithValidSubpath_ReturnsTrue()
    {
        // Arrange
        var basePath = @"C:\Users\Test\AppData";
        var filePath = @"C:\Users\Test\AppData\TwinShell\exports\file.json";

        // Act
        var result = PathValidator.IsPathWithinDirectory(filePath, basePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPathWithinDirectory_WithPathTraversal_ReturnsFalse()
    {
        // Arrange
        var basePath = @"C:\Users\Test\AppData\TwinShell";
        var filePath = @"C:\Users\Test\AppData\TwinShell\..\..\..\Windows\file.json";

        // Act
        var result = PathValidator.IsPathWithinDirectory(filePath, basePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPathWithinDirectory_WithTildeCharacter_ReturnsFalse()
    {
        // Arrange
        var basePath = @"C:\Users\Test\AppData";
        var filePath = @"C:\Users\Test\AppData\~\file.json";

        // Act
        var result = PathValidator.IsPathWithinDirectory(filePath, basePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPathWithinDirectory_WithPathOutsideBase_ReturnsFalse()
    {
        // Arrange
        var basePath = @"C:\Users\Test\AppData\TwinShell";
        var filePath = @"C:\Windows\System32\file.json";

        // Act
        var result = PathValidator.IsPathWithinDirectory(filePath, basePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPathWithinDirectory_WithSimilarPrefixButDifferentFolder_ReturnsFalse()
    {
        // Arrange - Tests that "TwinShellMalicious" is not accepted when base is "TwinShell"
        var basePath = @"C:\Users\Test\AppData\TwinShell";
        var filePath = @"C:\Users\Test\AppData\TwinShellMalicious\file.json";

        // Act
        var result = PathValidator.IsPathWithinDirectory(filePath, basePath);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region HasValidExtension Tests

    [Fact]
    public void HasValidExtension_WithMatchingExtension_ReturnsTrue()
    {
        // Arrange
        var path = @"C:\data\file.json";

        // Act
        var result = PathValidator.HasValidExtension(path, ".json");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasValidExtension_WithMultipleAllowedExtensions_ReturnsTrue()
    {
        // Arrange
        var path = @"C:\data\file.xml";

        // Act
        var result = PathValidator.HasValidExtension(path, ".json", ".xml", ".csv");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasValidExtension_WithNonMatchingExtension_ReturnsFalse()
    {
        // Arrange
        var path = @"C:\data\file.exe";

        // Act
        var result = PathValidator.HasValidExtension(path, ".json", ".xml");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasValidExtension_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var path = @"C:\data\file.JSON";

        // Act
        var result = PathValidator.HasValidExtension(path, ".json");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasValidExtension_WithEmptyPath_ReturnsFalse()
    {
        // Act
        var result = PathValidator.HasValidExtension(string.Empty, ".json");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasValidExtension_WithNoAllowedExtensions_ReturnsFalse()
    {
        // Arrange
        var path = @"C:\data\file.json";

        // Act
        var result = PathValidator.HasValidExtension(path);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
