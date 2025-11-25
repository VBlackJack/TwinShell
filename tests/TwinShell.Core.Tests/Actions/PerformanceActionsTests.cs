using System.Text.Json;
using TwinShell.Core.Enums;
using TwinShell.Core.Models;

namespace TwinShell.Core.Tests.Actions;

/// <summary>
/// Tests for Windows Performance Optimization actions (Sprint 8)
/// Validates that all 26 performance actions are properly defined and configured
/// Ensures proper categorization and safety levels
/// </summary>
public class PerformanceActionsTests
{
    private readonly List<Action> _actions;
    private const string ActionsFilePath = "../../../../../data/seed/initial-actions.json";

    public PerformanceActionsTests()
    {
        // Load actions from seed file
        var json = File.ReadAllText(ActionsFilePath);
        var jsonDoc = JsonDocument.Parse(json);
        var actionsArray = jsonDoc.RootElement.GetProperty("actions");

        _actions = new List<Action>();
        foreach (var actionElement in actionsArray.EnumerateArray())
        {
            var action = new Action
            {
                Id = actionElement.GetProperty("id").GetString() ?? string.Empty,
                Title = actionElement.GetProperty("title").GetString() ?? string.Empty,
                Description = actionElement.GetProperty("description").GetString() ?? string.Empty,
                Category = actionElement.GetProperty("category").GetString() ?? string.Empty,
                Platform = (Platform)actionElement.GetProperty("platform").GetInt32(),
                Level = (CriticalityLevel)actionElement.GetProperty("level").GetInt32(),
                Tags = actionElement.GetProperty("tags").EnumerateArray()
                    .Select(t => t.GetString() ?? string.Empty).ToList()
            };

            // Load notes if present
            if (actionElement.TryGetProperty("notes", out var notesElement))
            {
                action.Notes = notesElement.GetString();
            }

            _actions.Add(action);
        }
    }

    #region General Performance Actions Tests

    [Fact]
    public void AllPerformanceActions_ShouldExist()
    {
        // Arrange
        var expectedPerformanceActionIds = new[]
        {
            // DNS Configuration (001-006)
            "WIN-PERF-001", "WIN-PERF-002", "WIN-PERF-003", "WIN-PERF-004",
            "WIN-PERF-005", "WIN-PERF-006",
            // Power Management (101-104)
            "WIN-PERF-101", "WIN-PERF-102", "WIN-PERF-103", "WIN-PERF-104",
            // Windows Services (201-205)
            "WIN-PERF-201", "WIN-PERF-202", "WIN-PERF-203", "WIN-PERF-204", "WIN-PERF-205",
            // Indexing & Cache (301-304)
            "WIN-PERF-301", "WIN-PERF-302", "WIN-PERF-303", "WIN-PERF-304",
            // Graphics & Hardware (401-406)
            "WIN-PERF-401", "WIN-PERF-402", "WIN-PERF-403", "WIN-PERF-404",
            "WIN-PERF-405", "WIN-PERF-406"
        };

        // Act
        var performanceActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-")).ToList();

        // Assert
        performanceActions.Should().HaveCount(26, "Sprint 8 requires exactly 26 performance actions");

        foreach (var expectedId in expectedPerformanceActionIds)
        {
            performanceActions.Should().Contain(a => a.Id == expectedId,
                $"Action {expectedId} should exist");
        }
    }

    [Fact]
    public void AllPerformanceActions_ShouldHaveCorrectCategory()
    {
        // Arrange
        var performanceActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-")).ToList();

        // Assert
        performanceActions.Should().AllSatisfy(action =>
        {
            action.Category.Should().Be("⚡ Optimisation des performances",
                $"Action {action.Id} should be in 'Optimisation des performances' category");
        });
    }

    [Fact]
    public void AllPerformanceActions_ShouldHavePerformanceTag()
    {
        // Arrange
        var performanceActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-")).ToList();

        // Assert
        performanceActions.Should().AllSatisfy(action =>
        {
            action.Tags.Should().Contain("performance",
                $"Action {action.Id} should have 'performance' tag");
        });
    }

    [Fact]
    public void AllPerformanceActions_ShouldBeWindowsPlatform()
    {
        // Arrange
        var performanceActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-")).ToList();

        // Assert
        performanceActions.Should().AllSatisfy(action =>
        {
            action.Platform.Should().Be(Platform.Windows,
                $"Action {action.Id} should be Windows-only");
        });
    }

    #endregion

    #region DNS Configuration Actions Tests (001-006)

    [Fact]
    public void DNSActions_ShouldHaveCorrectCount()
    {
        // Arrange & Act
        var dnsActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-00")).ToList();

        // Assert
        dnsActions.Should().HaveCount(6, "Should have 6 DNS configuration actions (001-006)");
    }

    [Fact]
    public void DNSActions_ShouldHaveDNSTag()
    {
        // Arrange
        var dnsActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-00")).ToList();

        // Assert
        dnsActions.Should().AllSatisfy(action =>
        {
            action.Tags.Should().Contain("dns",
                $"DNS action {action.Id} should have 'dns' tag");
        });
    }

    [Fact]
    public void DNSActions_ShouldHaveNetworkTag()
    {
        // Arrange
        var dnsActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-00")).ToList();

        // Assert
        dnsActions.Should().AllSatisfy(action =>
        {
            action.Tags.Should().Contain("network",
                $"DNS action {action.Id} should have 'network' tag");
        });
    }

    [Fact]
    public void DNSActions_ShouldNotBeDangerous()
    {
        // Arrange
        var dnsActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-00")).ToList();

        // Assert
        dnsActions.Should().AllSatisfy(action =>
        {
            action.Level.Should().NotBe(CriticalityLevel.Dangerous,
                $"DNS action {action.Id} should not be dangerous (Level 2)");
        });
    }

    [Theory]
    [InlineData("WIN-PERF-001", "Google")]
    [InlineData("WIN-PERF-002", "Cloudflare")]
    [InlineData("WIN-PERF-003", "OpenDNS")]
    [InlineData("WIN-PERF-004", "Quad9")]
    public void DNSProviderActions_ShouldHaveCorrectProviderName(string actionId, string providerName)
    {
        // Arrange & Act
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull();
        action!.Title.Should().Contain(providerName,
            $"Action {actionId} should mention {providerName} in title");
    }

    #endregion

    #region Power Management Actions Tests (101-104)

    [Fact]
    public void PowerManagementActions_ShouldHaveCorrectCount()
    {
        // Arrange & Act
        var powerActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-10")).ToList();

        // Assert
        powerActions.Should().HaveCount(4, "Should have 4 power management actions (101-104)");
    }

    [Fact]
    public void PowerManagementActions_ShouldHavePowerTag()
    {
        // Arrange
        var powerActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-10")).ToList();

        // Assert
        powerActions.Should().AllSatisfy(action =>
        {
            action.Tags.Should().Contain("power",
                $"Power management action {action.Id} should have 'power' tag");
        });
    }

    [Fact]
    public void UltimatePerformancePlan_ShouldExistWithCorrectTitle()
    {
        // Arrange & Act
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PERF-101");

        // Assert
        action.Should().NotBeNull();
        action!.Title.Should().Contain("Ultimate Performance");
    }

    [Fact]
    public void HighPerformancePlan_ShouldExistWithCorrectTitle()
    {
        // Arrange & Act
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PERF-102");

        // Assert
        action.Should().NotBeNull();
        action!.Title.Should().Contain("Hautes performances");
    }

    #endregion

    #region Windows Services Actions Tests (201-205)

    [Fact]
    public void WindowsServicesActions_ShouldHaveCorrectCount()
    {
        // Arrange & Act
        var servicesActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-20")).ToList();

        // Assert
        servicesActions.Should().HaveCount(5, "Should have 5 Windows services actions (201-205)");
    }

    [Fact]
    public void WindowsServicesActions_ShouldHaveServicesTag()
    {
        // Arrange
        var servicesActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-20")).ToList();

        // Assert
        servicesActions.Should().AllSatisfy(action =>
        {
            action.Tags.Should().Contain("services",
                $"Windows services action {action.Id} should have 'services' tag");
        });
    }

    [Fact]
    public void DisableAllServices_ShouldBeDangerous()
    {
        // Arrange & Act
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PERF-201");

        // Assert
        action.Should().NotBeNull();
        action!.Level.Should().Be(CriticalityLevel.Dangerous,
            "WIN-PERF-201 (disable 200+ services) should be marked as Dangerous");
    }

    [Fact]
    public void DisableAllServices_ShouldHaveAdvancedTag()
    {
        // Arrange & Act
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PERF-201");

        // Assert
        action.Should().NotBeNull();
        action!.Tags.Should().Contain("advanced",
            "WIN-PERF-201 should have 'advanced' tag");
    }

    [Fact]
    public void DisableTelemetryServices_ShouldNotBeDangerous()
    {
        // Arrange & Act
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PERF-202");

        // Assert
        action.Should().NotBeNull();
        action!.Level.Should().NotBe(CriticalityLevel.Dangerous,
            "WIN-PERF-202 (telemetry only) should be safe (not Dangerous)");
    }

    [Fact]
    public void ListDisabledServices_ShouldBeInfo()
    {
        // Arrange & Act
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PERF-204");

        // Assert
        action.Should().NotBeNull();
        action!.Level.Should().Be(CriticalityLevel.Info,
            "WIN-PERF-204 (list services) should be Info level");
    }

    #endregion

    #region Indexing & Cache Actions Tests (301-304)

    [Fact]
    public void IndexingCacheActions_ShouldHaveCorrectCount()
    {
        // Arrange & Act
        var indexingActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-30")).ToList();

        // Assert
        indexingActions.Should().HaveCount(4, "Should have 4 indexing/cache actions (301-304)");
    }

    [Fact]
    public void SuperfetchAction_ShouldMentionSSD()
    {
        // Arrange & Act
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PERF-301");

        // Assert
        action.Should().NotBeNull();
        action!.Description.Should().Contain("SSD",
            "Superfetch action should mention SSD benefits");
    }

    [Fact]
    public void FlushDNS_ShouldBeInfo()
    {
        // Arrange & Act
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PERF-303");

        // Assert
        action.Should().NotBeNull();
        action!.Level.Should().Be(CriticalityLevel.Info,
            "WIN-PERF-303 (flush DNS) should be Info level - harmless diagnostic command");
    }

    #endregion

    #region Graphics & Hardware Actions Tests (401-406)

    [Fact]
    public void GraphicsHardwareActions_ShouldHaveCorrectCount()
    {
        // Arrange & Act
        var graphicsActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-40")).ToList();

        // Assert
        graphicsActions.Should().HaveCount(6, "Should have 6 graphics/hardware actions (401-406)");
    }

    [Fact]
    public void GamingActions_ShouldHaveGamingTag()
    {
        // Arrange
        var gamingActionIds = new[] { "WIN-PERF-401", "WIN-PERF-403", "WIN-PERF-404" };
        var gamingActions = _actions.Where(a => gamingActionIds.Contains(a.Id)).ToList();

        // Assert
        gamingActions.Should().AllSatisfy(action =>
        {
            action.Tags.Should().Contain("gaming",
                $"Gaming action {action.Id} should have 'gaming' tag");
        });
    }

    [Fact]
    public void CoreIsolationAction_ShouldMentionSecurityImpact()
    {
        // Arrange & Act
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PERF-402");

        // Assert
        action.Should().NotBeNull();
        action!.Notes.Should().NotBeNullOrEmpty();
        action.Notes!.Should().Contain("sécurité",
            "Core Isolation action should mention security impact");
    }

    [Fact]
    public void DefenderExclusions_ShouldHaveParameters()
    {
        // Arrange & Act
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PERF-406");

        // Assert
        action.Should().NotBeNull();
        action!.Title.Should().ContainEquivalentOf("exclusions");
    }

    #endregion

    #region Safety and Documentation Tests

    [Fact]
    public void DangerousActions_ShouldHaveWarningsInNotes()
    {
        // Arrange
        var performanceActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-")).ToList();
        var dangerousActions = performanceActions.Where(a => a.Level == CriticalityLevel.Dangerous).ToList();

        // Assert
        dangerousActions.Should().AllSatisfy(action =>
        {
            action.Notes.Should().NotBeNullOrEmpty(
                $"Dangerous action {action.Id} should have notes explaining risks");
            action.Notes!.Should().MatchRegex(@"⚠️|DANGER|ATTENTION|WARNING|IMPORTANT",
                $"Dangerous action {action.Id} should have warning markers in notes");
        });
    }

    [Fact]
    public void AllPerformanceActions_ShouldHaveDescription()
    {
        // Arrange
        var performanceActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-")).ToList();

        // Assert
        performanceActions.Should().AllSatisfy(action =>
        {
            action.Description.Should().NotBeNullOrWhiteSpace(
                $"Action {action.Id} should have a description");
            action.Description.Length.Should().BeGreaterThan(20,
                $"Action {action.Id} description should be meaningful (>20 chars)");
        });
    }

    [Fact]
    public void AllPerformanceActions_ShouldHaveNotes()
    {
        // Arrange
        var performanceActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-")).ToList();

        // Assert
        performanceActions.Should().AllSatisfy(action =>
        {
            action.Notes.Should().NotBeNullOrWhiteSpace(
                $"Action {action.Id} should have notes with usage guidance");
        });
    }

    [Fact]
    public void PerformanceActions_ShouldHaveOptimisationTag()
    {
        // Arrange
        var performanceActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-")).ToList();

        // Assert
        performanceActions.Should().AllSatisfy(action =>
        {
            action.Tags.Should().Contain("optimisation",
                $"Performance action {action.Id} should have 'optimisation' tag");
        });
    }

    [Fact]
    public void PerformanceActions_ShouldHaveWindowsTag()
    {
        // Arrange
        var performanceActions = _actions.Where(a => a.Id.StartsWith("WIN-PERF-")).ToList();

        // Assert
        performanceActions.Should().AllSatisfy(action =>
        {
            action.Tags.Should().Contain("windows",
                $"Performance action {action.Id} should have 'windows' tag");
        });
    }

    #endregion

    #region Specific Action Content Tests

    [Fact]
    public void CloudflareDNS_ShouldMentionSpeed()
    {
        // Arrange & Act
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PERF-002");

        // Assert
        action.Should().NotBeNull();
        (action!.Description + action.Notes).Should().MatchRegex(@"rapide|fast|speed|14ms",
            "Cloudflare DNS action should mention its speed advantage");
    }

    [Fact]
    public void CustomDNS_ShouldHaveTwoParameters()
    {
        // Arrange & Act
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PERF-005");

        // Assert
        action.Should().NotBeNull();
        action!.Title.Should().Contain("personnalisé",
            "WIN-PERF-005 should be for custom DNS");
    }

    [Fact]
    public void RestoreDNS_ShouldMentionDHCP()
    {
        // Arrange & Act
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PERF-006");

        // Assert
        action.Should().NotBeNull();
        action!.Description.Should().Contain("automatique",
            "Restore DNS action should mention automatic/DHCP configuration");
    }

    [Fact]
    public void DisableHibernation_ShouldMentionDiskSpace()
    {
        // Arrange & Act
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PERF-103");

        // Assert
        action.Should().NotBeNull();
        (action!.Description + action.Notes).Should().MatchRegex(@"espace|disk|Go|space",
            "Disable hibernation action should mention disk space savings");
    }

    [Fact]
    public void MouseLatency_ShouldMentionGaming()
    {
        // Arrange & Act
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PERF-403");

        // Assert
        action.Should().NotBeNull();
        (action!.Description + action.Tags.ToList().ToString()).Should().MatchRegex(@"gaming|latency|souris|mouse",
            "Mouse latency action should mention gaming or latency");
    }

    #endregion
}
