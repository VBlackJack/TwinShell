using System.Text.Json;
using TwinShell.Core.Enums;
using TwinShell.Core.Models;

namespace TwinShell.Core.Tests.Actions;

/// <summary>
/// Tests for Windows Debloating actions (Sprint 6)
/// Validates that all 22 debloating actions are properly defined and configured
/// </summary>
public class DebloatingActionsTests
{
    private readonly List<Action> _actions;
    private const string ActionsDirectoryPath = "../../../../../data/seed/actions";

    public DebloatingActionsTests()
    {
        // Load actions from individual JSON files (v1.5.0+ format)
        _actions = new List<Action>();

        var actionsDir = Path.GetFullPath(ActionsDirectoryPath);
        if (!Directory.Exists(actionsDir))
        {
            throw new DirectoryNotFoundException($"Actions directory not found: {actionsDir}");
        }

        foreach (var filePath in Directory.GetFiles(actionsDir, "*.json"))
        {
            var fileName = Path.GetFileName(filePath);
            if (fileName == "_index.json") continue; // Skip index file

            var json = File.ReadAllText(filePath);
            var actionElement = JsonDocument.Parse(json).RootElement;

            var action = new Action
            {
                Id = actionElement.GetProperty("id").GetString()?.ToUpperInvariant() ?? string.Empty,
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

    #region General Debloating Actions Tests

    [Fact]
    public void AllDebloatingActions_ShouldExist()
    {
        // Arrange
        var expectedDebloatingActionIds = new[]
        {
            // Bloatware Tiers
            "WIN-DEBLOAT-001", "WIN-DEBLOAT-002", "WIN-DEBLOAT-003",
            // Applications Microsoft
            "WIN-DEBLOAT-101", "WIN-DEBLOAT-102", "WIN-DEBLOAT-103", "WIN-DEBLOAT-104", "WIN-DEBLOAT-105",
            // Composants Système (DANGEROUS)
            "WIN-DEBLOAT-201", "WIN-DEBLOAT-202", "WIN-DEBLOAT-203", "WIN-DEBLOAT-204", "WIN-DEBLOAT-205", "WIN-DEBLOAT-206",
            // Fonctionnalités Windows
            "WIN-DEBLOAT-301", "WIN-DEBLOAT-302", "WIN-DEBLOAT-303", "WIN-DEBLOAT-304",
            // Optimisation Edge
            "WIN-DEBLOAT-401", "WIN-DEBLOAT-402", "WIN-DEBLOAT-403", "WIN-DEBLOAT-404"
        };

        // Act
        var debloatingActions = _actions.Where(a => a.Id.StartsWith("WIN-DEBLOAT-")).ToList();

        // Assert
        debloatingActions.Should().HaveCount(22, "Sprint 6 requires exactly 22 debloating actions");

        foreach (var expectedId in expectedDebloatingActionIds)
        {
            debloatingActions.Should().Contain(a => a.Id == expectedId,
                $"Action {expectedId} should exist");
        }
    }

    [Fact]
    public void AllDebloatingActions_ShouldHaveCorrectCategory()
    {
        // Arrange
        var debloatingActions = _actions.Where(a => a.Id.StartsWith("WIN-DEBLOAT-")).ToList();

        // Assert
        // Note: Category renamed from "Debloating Windows" to "Windows Debloat" in v1.4.0
        debloatingActions.Should().AllSatisfy(action =>
        {
            action.Category.Should().Be("🧹 Windows Debloat",
                $"Action {action.Id} should be in 'Windows Debloat' category");
        });
    }

    [Fact]
    public void AllDebloatingActions_ShouldBeWindowsOnly()
    {
        // Arrange
        var debloatingActions = _actions.Where(a => a.Id.StartsWith("WIN-DEBLOAT-")).ToList();

        // Assert
        debloatingActions.Should().AllSatisfy(action =>
        {
            action.Platform.Should().Be(Platform.Windows,
                $"Action {action.Id} should be Windows platform only");
        });
    }

    [Fact]
    public void AllDebloatingActions_ShouldHaveDebloatTag()
    {
        // Arrange
        var debloatingActions = _actions.Where(a => a.Id.StartsWith("WIN-DEBLOAT-")).ToList();

        // Assert
        debloatingActions.Should().AllSatisfy(action =>
        {
            action.Tags.Should().Contain("debloat",
                $"Action {action.Id} should have 'debloat' tag");
        });
    }

    #endregion

    #region Bloatware Tiers Actions Tests

    [Fact]
    public void BloatwareTiersActions_ShouldHaveCorrectCount()
    {
        // Arrange & Act
        var bloatwareActions = _actions.Where(a =>
            a.Id == "WIN-DEBLOAT-001" ||
            a.Id == "WIN-DEBLOAT-002" ||
            a.Id == "WIN-DEBLOAT-003").ToList();

        // Assert
        bloatwareActions.Should().HaveCount(3, "There should be 3 bloatware tiers actions");
    }

    [Theory]
    [InlineData("WIN-DEBLOAT-001", CriticalityLevel.Run)]
    [InlineData("WIN-DEBLOAT-002", CriticalityLevel.Run)]
    [InlineData("WIN-DEBLOAT-003", CriticalityLevel.Info)]
    public void BloatwareTiersActions_ShouldHaveCorrectLevel(string actionId, CriticalityLevel expectedLevel)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Level.Should().Be(expectedLevel,
            $"Action {actionId} should have level {expectedLevel}");
    }

    [Fact]
    public void WIN_DEBLOAT_003_ShouldBeInfoOnly()
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-DEBLOAT-003");

        // Assert
        action.Should().NotBeNull();
        action!.Level.Should().Be(CriticalityLevel.Info,
            "Listing apps should be informational only");
        action.Description.Should().ContainEquivalentOf("liste");
    }

    #endregion

    #region Microsoft Apps Actions Tests

    [Fact]
    public void MicrosoftAppsActions_ShouldHaveCorrectCount()
    {
        // Arrange & Act
        var microsoftAppsActions = _actions.Where(a =>
            a.Id.StartsWith("WIN-DEBLOAT-1")).ToList();

        // Assert
        microsoftAppsActions.Should().HaveCount(5, "There should be 5 Microsoft apps actions");
    }

    [Theory]
    [InlineData("WIN-DEBLOAT-101", "microsoft-apps")]
    [InlineData("WIN-DEBLOAT-102", "games")]
    [InlineData("WIN-DEBLOAT-103", "communication")]
    [InlineData("WIN-DEBLOAT-104", "weather")]
    [InlineData("WIN-DEBLOAT-105", "custom")]
    public void MicrosoftAppsActions_ShouldHaveSpecificTags(string actionId, string expectedTag)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Tags.Should().Contain(expectedTag,
            $"Action {actionId} should have '{expectedTag}' tag");
    }

    [Fact]
    public void WIN_DEBLOAT_101_ShouldTarget38Apps()
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-DEBLOAT-101");

        // Assert
        action.Should().NotBeNull();
        action!.Description.Should().Contain("38",
            "WIN-DEBLOAT-101 should mention targeting 38 apps");
    }

    #endregion

    #region System Components Actions Tests (DANGEROUS)

    [Fact]
    public void SystemComponentsActions_ShouldHaveCorrectCount()
    {
        // Arrange & Act
        var systemComponentActions = _actions.Where(a =>
            a.Id.StartsWith("WIN-DEBLOAT-2")).ToList();

        // Assert
        systemComponentActions.Should().HaveCount(6, "There should be 6 system component actions");
    }

    [Theory]
    [InlineData("WIN-DEBLOAT-201")]
    [InlineData("WIN-DEBLOAT-202")]
    [InlineData("WIN-DEBLOAT-203")]
    [InlineData("WIN-DEBLOAT-204")]
    [InlineData("WIN-DEBLOAT-205")]
    [InlineData("WIN-DEBLOAT-206")]
    public void SystemComponentsActions_ShouldAllBeDangerous(string actionId)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Level.Should().Be(CriticalityLevel.Dangerous,
            $"System component action {actionId} must be DANGEROUS level");
    }

    [Theory]
    [InlineData("WIN-DEBLOAT-201")]
    [InlineData("WIN-DEBLOAT-202")]
    [InlineData("WIN-DEBLOAT-203")]
    [InlineData("WIN-DEBLOAT-204")]
    [InlineData("WIN-DEBLOAT-205")]
    [InlineData("WIN-DEBLOAT-206")]
    public void SystemComponentsActions_ShouldHaveDangerousTag(string actionId)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Tags.Should().Contain("dangerous",
            $"System component action {actionId} should have 'dangerous' tag");
    }

    [Theory]
    [InlineData("WIN-DEBLOAT-201")]
    [InlineData("WIN-DEBLOAT-202")]
    [InlineData("WIN-DEBLOAT-203")]
    [InlineData("WIN-DEBLOAT-204")]
    [InlineData("WIN-DEBLOAT-205")]
    [InlineData("WIN-DEBLOAT-206")]
    public void SystemComponentsActions_ShouldHaveWarningInNotes(string actionId)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Notes.Should().NotBeNullOrEmpty($"Action {actionId} should have notes");
        action.Notes.Should().Contain("⚠️ ATTENTION",
            $"Dangerous action {actionId} should have warning in notes");
        action.Notes.Should().Contain("irréversible",
            $"Dangerous action {actionId} should mention irreversibility");
    }

    [Fact]
    public void WIN_DEBLOAT_202_OneDrive_ShouldMention30Steps()
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-DEBLOAT-202");

        // Assert
        action.Should().NotBeNull();
        action!.Description.Should().Contain("30+",
            "OneDrive removal should mention 30+ steps");
    }

    [Theory]
    [InlineData("WIN-DEBLOAT-201", "store")]
    [InlineData("WIN-DEBLOAT-202", "onedrive")]
    [InlineData("WIN-DEBLOAT-203", "edge")]
    [InlineData("WIN-DEBLOAT-204", "copilot")]
    [InlineData("WIN-DEBLOAT-205", "xbox")]
    [InlineData("WIN-DEBLOAT-206", "widgets")]
    public void SystemComponentsActions_ShouldHaveComponentTag(string actionId, string componentTag)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Tags.Should().Contain(componentTag,
            $"Action {actionId} should have '{componentTag}' tag");
    }

    #endregion

    #region Windows Features Actions Tests

    [Fact]
    public void WindowsFeaturesActions_ShouldHaveCorrectCount()
    {
        // Arrange & Act
        var windowsFeaturesActions = _actions.Where(a =>
            a.Id.StartsWith("WIN-DEBLOAT-3")).ToList();

        // Assert
        windowsFeaturesActions.Should().HaveCount(4, "There should be 4 Windows features actions");
    }

    [Theory]
    [InlineData("WIN-DEBLOAT-301", CriticalityLevel.Run)]
    [InlineData("WIN-DEBLOAT-302", CriticalityLevel.Run)]
    [InlineData("WIN-DEBLOAT-303", CriticalityLevel.Run)]
    [InlineData("WIN-DEBLOAT-304", CriticalityLevel.Run)]
    public void WindowsFeaturesActions_ShouldBeRunLevel(string actionId, CriticalityLevel expectedLevel)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Level.Should().Be(expectedLevel,
            $"Windows feature action {actionId} should be {expectedLevel} level");
    }

    [Fact]
    public void WIN_DEBLOAT_301_ConsumerFeatures_ShouldPreventBloatware()
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-DEBLOAT-301");

        // Assert
        action.Should().NotBeNull();
        action!.Description.Should().Contain("installation automatique",
            "Consumer Features should prevent automatic app installation");
    }

    #endregion

    #region Edge Optimization Actions Tests

    [Fact]
    public void EdgeOptimizationActions_ShouldHaveCorrectCount()
    {
        // Arrange & Act
        var edgeActions = _actions.Where(a =>
            a.Id.StartsWith("WIN-DEBLOAT-4")).ToList();

        // Assert
        edgeActions.Should().HaveCount(4, "There should be 4 Edge optimization actions");
    }

    [Theory]
    [InlineData("WIN-DEBLOAT-401")]
    [InlineData("WIN-DEBLOAT-402")]
    [InlineData("WIN-DEBLOAT-403")]
    [InlineData("WIN-DEBLOAT-404")]
    public void EdgeOptimizationActions_ShouldHaveEdgeTag(string actionId)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Tags.Should().Contain("edge",
            $"Edge optimization action {actionId} should have 'edge' tag");
    }

    [Theory]
    [InlineData("WIN-DEBLOAT-401", CriticalityLevel.Run)]
    [InlineData("WIN-DEBLOAT-402", CriticalityLevel.Run)]
    [InlineData("WIN-DEBLOAT-403", CriticalityLevel.Run)]
    [InlineData("WIN-DEBLOAT-404", CriticalityLevel.Run)]
    public void EdgeOptimizationActions_ShouldBeRunLevel(string actionId, CriticalityLevel expectedLevel)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Level.Should().Be(expectedLevel,
            $"Edge optimization action {actionId} should be {expectedLevel} level");
    }

    [Theory]
    [InlineData("WIN-DEBLOAT-401", "recommendations")]
    [InlineData("WIN-DEBLOAT-402", "shopping")]
    [InlineData("WIN-DEBLOAT-403", "telemetry")]
    [InlineData("WIN-DEBLOAT-404", "crypto")]
    public void EdgeOptimizationActions_ShouldHaveSpecificTags(string actionId, string expectedTag)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Tags.Should().Contain(expectedTag,
            $"Action {actionId} should have '{expectedTag}' tag");
    }

    [Fact]
    public void WIN_DEBLOAT_403_EdgeTelemetry_ShouldMentionPrivacy()
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-DEBLOAT-403");

        // Assert
        action.Should().NotBeNull();
        action!.Tags.Should().Contain("privacy",
            "Telemetry action should have privacy tag");
    }

    #endregion

    #region Criticality Level Distribution Tests

    [Fact]
    public void DebloatingActions_ShouldHaveCorrectCriticalityDistribution()
    {
        // Arrange
        var debloatingActions = _actions.Where(a => a.Id.StartsWith("WIN-DEBLOAT-")).ToList();

        // Act
        var infoActions = debloatingActions.Where(a => a.Level == CriticalityLevel.Info).Count();
        var runActions = debloatingActions.Where(a => a.Level == CriticalityLevel.Run).Count();
        var dangerousActions = debloatingActions.Where(a => a.Level == CriticalityLevel.Dangerous).Count();

        // Assert
        infoActions.Should().Be(1, "Should have 1 Info action (WIN-DEBLOAT-003)");
        runActions.Should().Be(15, "Should have 15 Run actions");
        dangerousActions.Should().Be(6, "Should have 6 Dangerous actions (system components)");
    }

    [Fact]
    public void DebloatingActions_OnlySystemComponents_ShouldBeDangerous()
    {
        // Arrange
        var dangerousActions = _actions.Where(a =>
            a.Id.StartsWith("WIN-DEBLOAT-") &&
            a.Level == CriticalityLevel.Dangerous).ToList();

        // Assert
        dangerousActions.Should().HaveCount(6);
        dangerousActions.Should().AllSatisfy(action =>
        {
            action.Id.Should().StartWith("WIN-DEBLOAT-2",
                "Only system component actions (2xx) should be dangerous");
        });
    }

    #endregion

    #region Data Quality Tests

    [Fact]
    public void AllDebloatingActions_ShouldHaveTitle()
    {
        // Arrange
        var debloatingActions = _actions.Where(a => a.Id.StartsWith("WIN-DEBLOAT-")).ToList();

        // Assert
        debloatingActions.Should().AllSatisfy(action =>
        {
            action.Title.Should().NotBeNullOrEmpty($"Action {action.Id} should have a title");
        });
    }

    [Fact]
    public void AllDebloatingActions_ShouldHaveDescription()
    {
        // Arrange
        var debloatingActions = _actions.Where(a => a.Id.StartsWith("WIN-DEBLOAT-")).ToList();

        // Assert
        debloatingActions.Should().AllSatisfy(action =>
        {
            action.Description.Should().NotBeNullOrEmpty(
                $"Action {action.Id} should have a description");
            action.Description.Length.Should().BeGreaterThan(20,
                $"Action {action.Id} description should be meaningful");
        });
    }

    [Fact]
    public void AllDebloatingActions_ShouldHaveAtLeastTwoTags()
    {
        // Arrange
        var debloatingActions = _actions.Where(a => a.Id.StartsWith("WIN-DEBLOAT-")).ToList();

        // Assert
        debloatingActions.Should().AllSatisfy(action =>
        {
            action.Tags.Should().HaveCountGreaterOrEqualTo(2,
                $"Action {action.Id} should have at least 2 tags");
        });
    }

    [Fact]
    public void DangerousDebloatingActions_ShouldHaveDangerousDescription()
    {
        // Arrange
        var dangerousActions = _actions.Where(a =>
            a.Id.StartsWith("WIN-DEBLOAT-") &&
            a.Level == CriticalityLevel.Dangerous).ToList();

        // Assert
        dangerousActions.Should().AllSatisfy(action =>
        {
            action.Description.Should().Contain("⚠️ DANGEROUS",
                $"Dangerous action {action.Id} should have warning in description");
        });
    }

    #endregion
}
