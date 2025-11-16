using FluentAssertions;
using TwinShell.Core.Enums;
using TwinShell.Core.Models;
using TwinShell.Core.Services;

namespace TwinShell.Core.Tests.Services;

public class SearchServiceTests
{
    private readonly SearchService _service;
    private readonly List<Action> _testActions;

    public SearchServiceTests()
    {
        _service = new SearchService();
        _testActions = new List<Action>
        {
            new Action
            {
                Id = "1",
                Title = "List AD Users",
                Description = "Lists all Active Directory users",
                Category = "Active Directory",
                Platform = Platform.Windows,
                Level = CriticalityLevel.Info,
                Tags = new List<string> { "ad", "users", "list" }
            },
            new Action
            {
                Id = "2",
                Title = "Reset User Password",
                Description = "Resets password for a specific user",
                Category = "Active Directory",
                Platform = Platform.Windows,
                Level = CriticalityLevel.Run,
                Tags = new List<string> { "ad", "password", "reset" }
            },
            new Action
            {
                Id = "3",
                Title = "Query DNS Record",
                Description = "Queries DNS records for a domain",
                Category = "DNS",
                Platform = Platform.Both,
                Level = CriticalityLevel.Info,
                Tags = new List<string> { "dns", "query", "diagnostic" },
                Notes = "Useful for troubleshooting DNS issues"
            }
        };
    }

    [Fact]
    public async Task SearchAsync_WithEmptySearchTerm_ReturnsAllActions()
    {
        // Act
        var result = await _service.SearchAsync(_testActions, "");

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchAsync_WithWhitespaceSearchTerm_ReturnsAllActions()
    {
        // Act
        var result = await _service.SearchAsync(_testActions, "   ");

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchAsync_ByTitle_ReturnsMatchingActions()
    {
        // Act
        var result = await _service.SearchAsync(_testActions, "password");

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Reset User Password");
    }

    [Fact]
    public async Task SearchAsync_ByDescription_ReturnsMatchingActions()
    {
        // Act
        var result = await _service.SearchAsync(_testActions, "directory");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(a => a.Id == "1");
        result.Should().Contain(a => a.Id == "2");
    }

    [Fact]
    public async Task SearchAsync_ByCategory_ReturnsMatchingActions()
    {
        // Act
        var result = await _service.SearchAsync(_testActions, "dns");

        // Assert
        result.Should().HaveCount(1);
        result.First().Category.Should().Be("DNS");
    }

    [Fact]
    public async Task SearchAsync_ByTag_ReturnsMatchingActions()
    {
        // Act
        var result = await _service.SearchAsync(_testActions, "reset");

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be("2");
    }

    [Fact]
    public async Task SearchAsync_ByNotes_ReturnsMatchingActions()
    {
        // Act
        var result = await _service.SearchAsync(_testActions, "troubleshooting");

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be("3");
    }

    [Fact]
    public async Task SearchAsync_CaseInsensitive_ReturnsMatchingActions()
    {
        // Act
        var result = await _service.SearchAsync(_testActions, "PASSWORD");

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Contain("Password");
    }

    [Fact]
    public async Task SearchAsync_PartialMatch_ReturnsMatchingActions()
    {
        // Act
        var result = await _service.SearchAsync(_testActions, "pass");

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAsync_NoMatches_ReturnsEmptyList()
    {
        // Act
        var result = await _service.SearchAsync(_testActions, "nonexistent");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_MultipleMatches_ReturnsAllMatching()
    {
        // Act
        var result = await _service.SearchAsync(_testActions, "ad");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(a => a.Id == "1");
        result.Should().Contain(a => a.Id == "2");
    }
}
