using FluentAssertions;
using TwinShell.Core.Enums;
using TwinShell.Core.Models;
using TwinShell.Core.Services;

namespace TwinShell.Core.Tests.Services;

/// <summary>
/// Comprehensive tests for SearchService covering:
/// - Basic search (title, description, category, tags, notes)
/// - Case-insensitive search
/// - Accent/diacritic normalization
/// - Punctuation normalization (hyphens, underscores, dots)
/// - Multi-word search with AND logic
/// - Command template search
/// </summary>
public class SearchServiceTests
{
    private readonly SearchService _service;
    private readonly List<Action> _testActions;

    public SearchServiceTests()
    {
        _service = new SearchService();
        _testActions = new List<Action>
        {
            // Action 1: Standard PowerShell command with hyphen
            new Action
            {
                Id = "1",
                Title = "Get-Service",
                Description = "Lists all Windows services",
                Category = "Windows Services",
                Platform = Platform.Windows,
                Level = CriticalityLevel.Info,
                Tags = new List<string> { "service", "windows", "list" },
                WindowsCommandTemplate = new CommandTemplate
                {
                    Id = "tpl-1",
                    Name = "Get-Service",
                    CommandPattern = "Get-Service -Name {ServiceName}"
                }
            },
            // Action 2: Active Directory command with underscore
            new Action
            {
                Id = "2",
                Title = "List_AD_Users",
                Description = "Lists all Active Directory users",
                Category = "Active Directory",
                Platform = Platform.Windows,
                Level = CriticalityLevel.Info,
                Tags = new List<string> { "ad", "users", "list" }
            },
            // Action 3: French command with accents
            new Action
            {
                Id = "3",
                Title = "Configuration Réseau",
                Description = "Configure le réseau local et les paramètres WiFi",
                Category = "Réseau",
                Platform = Platform.Both,
                Level = CriticalityLevel.Run,
                Tags = new List<string> { "réseau", "wifi", "configuration" },
                Notes = "Nécessite les privilèges administrateur"
            },
            // Action 4: Standard password reset
            new Action
            {
                Id = "4",
                Title = "Reset User Password",
                Description = "Resets password for a specific user account",
                Category = "Active Directory",
                Platform = Platform.Windows,
                Level = CriticalityLevel.Run,
                Tags = new List<string> { "ad", "password", "reset" }
            },
            // Action 5: DNS query command
            new Action
            {
                Id = "5",
                Title = "Query DNS Record",
                Description = "Queries DNS records for a domain",
                Category = "DNS",
                Platform = Platform.Both,
                Level = CriticalityLevel.Info,
                Tags = new List<string> { "dns", "query", "diagnostic" },
                Notes = "Useful for troubleshooting DNS issues"
            },
            // Action 6: Linux command with dots
            new Action
            {
                Id = "6",
                Title = "System.Management.Check",
                Description = "Check system management status",
                Category = "Linux System",
                Platform = Platform.Linux,
                Level = CriticalityLevel.Info,
                Tags = new List<string> { "linux", "system", "check" },
                LinuxCommandTemplate = new CommandTemplate
                {
                    Id = "tpl-6",
                    Name = "systemctl status",
                    CommandPattern = "systemctl status {ServiceName}"
                }
            },
            // Action 7: Spanish command with accents
            new Action
            {
                Id = "7",
                Title = "Configuración del Sistema",
                Description = "Configuración avanzada del sistema operativo",
                Category = "Sistema",
                Platform = Platform.Both,
                Level = CriticalityLevel.Run,
                Tags = new List<string> { "configuración", "sistema" }
            }
        };
    }

    #region Basic Search Tests

    [Fact]
    public async Task SearchAsync_WithEmptySearchTerm_ReturnsAllActions()
    {
        // Act
        var result = await _service.SearchAsync(_testActions, "");

        // Assert
        result.Should().HaveCount(7);
    }

    [Fact]
    public async Task SearchAsync_WithWhitespaceSearchTerm_ReturnsAllActions()
    {
        // Act
        var result = await _service.SearchAsync(_testActions, "   ");

        // Assert
        result.Should().HaveCount(7);
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
        result.Should().HaveCount(2); // Actions 2 and 4
        result.Should().Contain(a => a.Id == "2");
        result.Should().Contain(a => a.Id == "4");
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
        result.First().Id.Should().Be("4");
    }

    [Fact]
    public async Task SearchAsync_ByNotes_ReturnsMatchingActions()
    {
        // Act
        var result = await _service.SearchAsync(_testActions, "troubleshooting");

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be("5");
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
        // Act - Search for "ad" should match Active Directory actions
        var result = await _service.SearchAsync(_testActions, "ad");

        // Assert
        result.Should().HaveCount(2); // Actions 2 and 4
        result.Should().Contain(a => a.Id == "2");
        result.Should().Contain(a => a.Id == "4");
    }

    #endregion

    #region Punctuation Normalization Tests

    [Fact]
    public async Task SearchAsync_WithHyphen_MatchesWithoutHyphen()
    {
        // Arrange - Action 1 has "Get-Service", search without hyphen

        // Act
        var result = await _service.SearchAsync(_testActions, "Get Service");

        // Assert
        result.Should().Contain(a => a.Id == "1", "Search 'Get Service' should match 'Get-Service'");
    }

    [Fact]
    public async Task SearchAsync_WithoutHyphen_MatchesWithHyphen()
    {
        // Arrange - Search with hyphen for action without hyphen

        // Act
        var result = await _service.SearchAsync(_testActions, "Get-Service");

        // Assert
        result.Should().Contain(a => a.Id == "1", "Search 'Get-Service' should match 'Get-Service'");
    }

    [Fact]
    public async Task SearchAsync_WithUnderscore_MatchesWithoutUnderscore()
    {
        // Arrange - Action 2 has "List_AD_Users", search without underscore

        // Act
        var result = await _service.SearchAsync(_testActions, "List AD Users");

        // Assert
        result.Should().Contain(a => a.Id == "2", "Search 'List AD Users' should match 'List_AD_Users'");
    }

    [Fact]
    public async Task SearchAsync_WithDots_MatchesWithoutDots()
    {
        // Arrange - Action 6 has "System.Management.Check", search without dots

        // Act
        var result = await _service.SearchAsync(_testActions, "System Management");

        // Assert
        result.Should().Contain(a => a.Id == "6", "Search 'System Management' should match 'System.Management.Check'");
    }

    [Fact]
    public async Task SearchAsync_PowerShellCommand_AllVariations()
    {
        // Arrange - Test all variations of "Get-Service"
        var queries = new[] { "Get-Service", "Get Service", "get-service", "GET-SERVICE", "GetService", "get service" };

        foreach (var query in queries)
        {
            // Act
            var result = await _service.SearchAsync(_testActions, query);

            // Assert
            result.Should().Contain(a => a.Id == "1", $"Query '{query}' should match 'Get-Service'");
        }
    }

    #endregion

    #region Accent/Diacritic Normalization Tests

    [Fact]
    public async Task SearchAsync_WithAccents_MatchesWithoutAccents()
    {
        // Arrange - Action 3 has "réseau" in title, search without accent

        // Act
        var result = await _service.SearchAsync(_testActions, "reseau");

        // Assert
        result.Should().Contain(a => a.Id == "3", "Search 'reseau' should match 'réseau'");
    }

    [Fact]
    public async Task SearchAsync_WithoutAccents_MatchesWithAccents()
    {
        // Arrange - Search with accents for "réseau"

        // Act
        var result = await _service.SearchAsync(_testActions, "réseau");

        // Assert
        result.Should().Contain(a => a.Id == "3", "Search 'réseau' should match 'réseau'");
    }

    [Fact]
    public async Task SearchAsync_FrenchAccents_AllVariations()
    {
        // Arrange - Test all variations of "réseau"
        var queries = new[] { "réseau", "reseau", "RÉSEAU", "RESEAU", "Réseau", "Reseau" };

        foreach (var query in queries)
        {
            // Act
            var result = await _service.SearchAsync(_testActions, query);

            // Assert
            result.Should().Contain(a => a.Id == "3", $"Query '{query}' should match 'Configuration Réseau'");
        }
    }

    [Fact]
    public async Task SearchAsync_SpanishAccents_AllVariations()
    {
        // Arrange - Action 7 has "Configuración" in Spanish
        var queries = new[] { "configuración", "configuracion", "CONFIGURACIÓN", "CONFIGURACION" };

        foreach (var query in queries)
        {
            // Act
            var result = await _service.SearchAsync(_testActions, query);

            // Assert
            result.Should().Contain(a => a.Id == "7", $"Query '{query}' should match 'Configuración del Sistema'");
        }
    }

    [Fact]
    public async Task SearchAsync_AccentsInDescription_MatchesWithoutAccents()
    {
        // Arrange - Action 3 has "paramètres" in description

        // Act
        var result = await _service.SearchAsync(_testActions, "parametres");

        // Assert
        result.Should().Contain(a => a.Id == "3", "Search 'parametres' should match 'paramètres'");
    }

    [Fact]
    public async Task SearchAsync_AccentsInTags_MatchesWithoutAccents()
    {
        // Arrange - Action 3 has "réseau" in tags

        // Act
        var result = await _service.SearchAsync(_testActions, "reseau");

        // Assert
        result.Should().Contain(a => a.Id == "3", "Search 'reseau' should match tag 'réseau'");
    }

    [Fact]
    public async Task SearchAsync_AccentsInNotes_MatchesWithoutAccents()
    {
        // Arrange - Action 3 has "Nécessite" in notes

        // Act
        var result = await _service.SearchAsync(_testActions, "necessite");

        // Assert
        result.Should().Contain(a => a.Id == "3", "Search 'necessite' should match notes 'Nécessite'");
    }

    #endregion

    #region Multi-Word Search Tests (AND Logic)

    [Fact]
    public async Task SearchAsync_MultiWord_AllWordsMatch_ReturnsResult()
    {
        // Arrange - Action 4: "Reset User Password"

        // Act
        var result = await _service.SearchAsync(_testActions, "user password");

        // Assert
        result.Should().Contain(a => a.Id == "4", "Both 'user' and 'password' are present");
    }

    [Fact]
    public async Task SearchAsync_MultiWord_OneWordMissing_ReturnsEmpty()
    {
        // Arrange - Search for "user firewall" (firewall doesn't exist)

        // Act
        var result = await _service.SearchAsync(_testActions, "user firewall");

        // Assert
        result.Should().NotContain(a => a.Id == "4", "'firewall' is not present in action");
    }

    [Fact]
    public async Task SearchAsync_MultiWord_OrderDoesNotMatter()
    {
        // Arrange - Action 2: "List_AD_Users" with description "Lists all Active Directory users"

        // Act - Search in different order
        var result1 = await _service.SearchAsync(_testActions, "directory active");
        var result2 = await _service.SearchAsync(_testActions, "active directory");

        // Assert
        result1.Should().Contain(a => a.Id == "2");
        result2.Should().Contain(a => a.Id == "2");
        result1.Should().HaveCount(result2.Count());
    }

    [Fact]
    public async Task SearchAsync_MultiWord_MatchesAcrossFields()
    {
        // Arrange - Action 1: Title="Get-Service", Description="Lists all Windows services"

        // Act - "service windows" should match (service from title, windows from description)
        var result = await _service.SearchAsync(_testActions, "service windows");

        // Assert
        result.Should().Contain(a => a.Id == "1", "'service' in title and 'windows' in description");
    }

    [Fact]
    public async Task SearchAsync_ThreeWords_AllMustMatch()
    {
        // Arrange - Action 2: "List_AD_Users", "Lists all Active Directory users"

        // Act
        var result = await _service.SearchAsync(_testActions, "list active directory");

        // Assert
        result.Should().Contain(a => a.Id == "2", "All three words are present");
    }

    [Fact]
    public async Task SearchAsync_ThreeWords_OneMissing_NoMatch()
    {
        // Arrange

        // Act - "list active firewall" (firewall missing)
        var result = await _service.SearchAsync(_testActions, "list active firewall");

        // Assert
        result.Should().NotContain(a => a.Id == "2", "'firewall' is not present");
    }

    #endregion

    #region Command Template Search Tests

    [Fact]
    public async Task SearchAsync_WindowsTemplate_ByName_ReturnsMatch()
    {
        // Arrange - Action 1 has WindowsCommandTemplate.Name = "Get-Service"

        // Act
        var result = await _service.SearchAsync(_testActions, "Get-Service");

        // Assert
        result.Should().Contain(a => a.Id == "1");
    }

    [Fact]
    public async Task SearchAsync_WindowsTemplate_ByCommandPattern_ReturnsMatch()
    {
        // Arrange - Action 1 has CommandPattern = "Get-Service -Name {ServiceName}"

        // Act
        var result = await _service.SearchAsync(_testActions, "ServiceName");

        // Assert
        result.Should().Contain(a => a.Id == "1", "ServiceName is in CommandPattern");
    }

    [Fact]
    public async Task SearchAsync_LinuxTemplate_ByName_ReturnsMatch()
    {
        // Arrange - Action 6 has LinuxCommandTemplate.Name = "systemctl status"

        // Act
        var result = await _service.SearchAsync(_testActions, "systemctl");

        // Assert
        result.Should().Contain(a => a.Id == "6");
    }

    [Fact]
    public async Task SearchAsync_LinuxTemplate_ByCommandPattern_ReturnsMatch()
    {
        // Arrange - Action 6 has CommandPattern = "systemctl status {ServiceName}"

        // Act
        var result = await _service.SearchAsync(_testActions, "systemctl status");

        // Assert
        result.Should().Contain(a => a.Id == "6");
    }

    #endregion

    #region Integration/Real-World Scenario Tests

    [Fact]
    public async Task RealWorld_SearchForService_FindsGetService()
    {
        // Scenario: User searches for "service" and expects to find "Get-Service" command

        // Act
        var result = await _service.SearchAsync(_testActions, "service");

        // Assert
        result.Should().Contain(a => a.Id == "1", "Get-Service contains 'service' in title");
    }

    [Fact]
    public async Task RealWorld_SearchForADUser_FindsListADUsers()
    {
        // Scenario: User searches for "AD User" and expects to find "List_AD_Users"

        // Act
        var result = await _service.SearchAsync(_testActions, "AD User");

        // Assert
        result.Should().Contain(a => a.Id == "2", "List_AD_Users matches 'AD' and 'User'");
    }

    [Fact]
    public async Task RealWorld_SearchForNetworkConfig_FindsFrenchCommand()
    {
        // Scenario: User searches for "reseau" (without accent) and expects to find "Configuration Réseau"

        // Act
        var result = await _service.SearchAsync(_testActions, "reseau");

        // Assert
        result.Should().Contain(a => a.Id == "3", "Should match 'réseau' even without accent");
    }

    [Fact]
    public async Task RealWorld_SearchForWiFi_FindsFrenchCommand()
    {
        // Scenario: User searches for "wifi" and expects to find French network configuration

        // Act
        var result = await _service.SearchAsync(_testActions, "wifi");

        // Assert
        result.Should().Contain(a => a.Id == "3", "WiFi is mentioned in description");
    }

    [Fact]
    public async Task RealWorld_SearchForSystemManagement_FindsLinuxCommand()
    {
        // Scenario: User searches for "system management" and expects to find "System.Management.Check"

        // Act
        var result = await _service.SearchAsync(_testActions, "system management");

        // Assert
        result.Should().Contain(a => a.Id == "6", "Should normalize dots to spaces");
    }

    #endregion
}
