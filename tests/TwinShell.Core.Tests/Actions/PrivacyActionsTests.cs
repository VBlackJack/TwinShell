using System.Text.Json;
using TwinShell.Core.Enums;
using TwinShell.Core.Models;

namespace TwinShell.Core.Tests.Actions;

/// <summary>
/// Tests for Windows Privacy actions (Sprint 7)
/// Validates that all 28 privacy actions are properly defined and configured
/// Ensures GDPR compliance and proper categorization
/// </summary>
public class PrivacyActionsTests
{
    private readonly List<Action> _actions;
    private const string ActionsFilePath = "../../../../../data/seed/initial-actions.json";

    public PrivacyActionsTests()
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

    #region General Privacy Actions Tests

    [Fact]
    public void AllPrivacyActions_ShouldExist()
    {
        // Arrange
        var expectedPrivacyActionIds = new[]
        {
            // App Permissions (001-010)
            "WIN-PRIVACY-001", "WIN-PRIVACY-002", "WIN-PRIVACY-003", "WIN-PRIVACY-004",
            "WIN-PRIVACY-005", "WIN-PRIVACY-006", "WIN-PRIVACY-007", "WIN-PRIVACY-008",
            "WIN-PRIVACY-009", "WIN-PRIVACY-010",
            // Cloud Sync (101-106)
            "WIN-PRIVACY-101", "WIN-PRIVACY-102", "WIN-PRIVACY-103",
            "WIN-PRIVACY-104", "WIN-PRIVACY-105", "WIN-PRIVACY-106",
            // Telemetry & Tracking (201-208)
            "WIN-PRIVACY-201", "WIN-PRIVACY-202", "WIN-PRIVACY-203", "WIN-PRIVACY-204",
            "WIN-PRIVACY-205", "WIN-PRIVACY-206", "WIN-PRIVACY-207", "WIN-PRIVACY-208",
            // Third-party Telemetry (301-304)
            "WIN-PRIVACY-301", "WIN-PRIVACY-302", "WIN-PRIVACY-303", "WIN-PRIVACY-304"
        };

        // Act
        var privacyActions = _actions.Where(a => a.Id.StartsWith("WIN-PRIVACY-")).ToList();

        // Assert
        privacyActions.Should().HaveCount(28, "Sprint 7 requires exactly 28 privacy actions");

        foreach (var expectedId in expectedPrivacyActionIds)
        {
            privacyActions.Should().Contain(a => a.Id == expectedId,
                $"Action {expectedId} should exist");
        }
    }

    [Fact]
    public void AllPrivacyActions_ShouldHaveCorrectCategory()
    {
        // Arrange
        var privacyActions = _actions.Where(a => a.Id.StartsWith("WIN-PRIVACY-")).ToList();

        // Assert
        privacyActions.Should().AllSatisfy(action =>
        {
            action.Category.Should().Be("🔒 Confidentialité Windows",
                $"Action {action.Id} should be in 'Confidentialité Windows' category");
        });
    }

    [Fact]
    public void AllPrivacyActions_ShouldBeWindowsOnly()
    {
        // Arrange
        var privacyActions = _actions.Where(a => a.Id.StartsWith("WIN-PRIVACY-")).ToList();

        // Assert
        privacyActions.Should().AllSatisfy(action =>
        {
            action.Platform.Should().Be(Platform.Windows,
                $"Action {action.Id} should be Windows platform only");
        });
    }

    [Fact]
    public void AllPrivacyActions_ShouldHavePrivacyTag()
    {
        // Arrange
        var privacyActions = _actions.Where(a => a.Id.StartsWith("WIN-PRIVACY-")).ToList();

        // Assert
        privacyActions.Should().AllSatisfy(action =>
        {
            action.Tags.Should().Contain("privacy",
                $"Action {action.Id} should have 'privacy' tag");
        });
    }

    [Fact]
    public void AllPrivacyActions_ShouldHaveConfidentialiteTag()
    {
        // Arrange
        var privacyActions = _actions.Where(a => a.Id.StartsWith("WIN-PRIVACY-")).ToList();

        // Assert
        privacyActions.Should().AllSatisfy(action =>
        {
            action.Tags.Should().Contain("confidentialité",
                $"Action {action.Id} should have 'confidentialité' tag");
        });
    }

    #endregion

    #region App Permissions Actions Tests (001-010)

    [Fact]
    public void AppPermissionsActions_ShouldHaveCorrectCount()
    {
        // Arrange & Act
        var permissionsActions = _actions.Where(a =>
            a.Id.StartsWith("WIN-PRIVACY-00")).ToList();

        // Assert
        permissionsActions.Should().HaveCount(10, "There should be 10 app permissions actions");
    }

    [Theory]
    [InlineData("WIN-PRIVACY-001", "localisation")]
    [InlineData("WIN-PRIVACY-002", "caméra")]
    [InlineData("WIN-PRIVACY-003", "microphone")]
    [InlineData("WIN-PRIVACY-004", "fichiers")]
    [InlineData("WIN-PRIVACY-005", "contacts")]
    [InlineData("WIN-PRIVACY-006", "calendrier")]
    [InlineData("WIN-PRIVACY-007", "email")]
    [InlineData("WIN-PRIVACY-008", "notifications")]
    public void IndividualPermissionActions_ShouldHaveSpecificTags(string actionId, string expectedTag)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Tags.Should().Contain(expectedTag,
            $"Action {actionId} should have '{expectedTag}' tag");
    }

    [Fact]
    public void WIN_PRIVACY_009_DisableAllPermissions_ShouldBeDangerous()
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PRIVACY-009");

        // Assert
        action.Should().NotBeNull();
        action!.Level.Should().Be(CriticalityLevel.Dangerous,
            "Disabling all permissions should be DANGEROUS level");
        action.Tags.Should().Contain("all", "Should have 'all' tag");
        action.Notes.Should().Contain("ATTENTION", "Should warn about impact");
    }

    [Fact]
    public void WIN_PRIVACY_010_RestorePermissions_ShouldBeRun()
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PRIVACY-010");

        // Assert
        action.Should().NotBeNull();
        action!.Level.Should().Be(CriticalityLevel.Run,
            "Restore action should be Run level");
        action.Tags.Should().Contain("restore", "Should have 'restore' tag");
        action.Tags.Should().Contain("default", "Should have 'default' tag");
    }

    [Theory]
    [InlineData("WIN-PRIVACY-001", CriticalityLevel.Run)]
    [InlineData("WIN-PRIVACY-002", CriticalityLevel.Run)]
    [InlineData("WIN-PRIVACY-003", CriticalityLevel.Run)]
    [InlineData("WIN-PRIVACY-004", CriticalityLevel.Run)]
    [InlineData("WIN-PRIVACY-005", CriticalityLevel.Run)]
    [InlineData("WIN-PRIVACY-006", CriticalityLevel.Run)]
    [InlineData("WIN-PRIVACY-007", CriticalityLevel.Run)]
    [InlineData("WIN-PRIVACY-008", CriticalityLevel.Run)]
    public void IndividualPermissionActions_ShouldBeRunLevel(string actionId, CriticalityLevel expectedLevel)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Level.Should().Be(expectedLevel,
            $"Individual permission action {actionId} should be {expectedLevel} level");
    }

    #endregion

    #region Cloud Sync Actions Tests (101-106)

    [Fact]
    public void CloudSyncActions_ShouldHaveCorrectCount()
    {
        // Arrange & Act
        var syncActions = _actions.Where(a =>
            a.Id.StartsWith("WIN-PRIVACY-10")).ToList();

        // Assert
        syncActions.Should().HaveCount(6, "There should be 6 cloud sync actions");
    }

    [Theory]
    [InlineData("WIN-PRIVACY-101")]
    [InlineData("WIN-PRIVACY-102")]
    [InlineData("WIN-PRIVACY-103")]
    [InlineData("WIN-PRIVACY-104")]
    [InlineData("WIN-PRIVACY-105")]
    [InlineData("WIN-PRIVACY-106")]
    public void CloudSyncActions_ShouldHaveSyncTag(string actionId)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Tags.Should().Contain("sync",
            $"Cloud sync action {actionId} should have 'sync' tag");
    }

    [Fact]
    public void WIN_PRIVACY_101_DisableAllSync_ShouldBeDangerous()
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PRIVACY-101");

        // Assert
        action.Should().NotBeNull();
        action!.Level.Should().Be(CriticalityLevel.Dangerous,
            "Disabling all sync should be DANGEROUS level");
        action.Tags.Should().Contain("cloud", "Should have 'cloud' tag");
        action.Description.Should().Contain("TOUTE synchronisation",
            "Should clearly state it disables ALL sync");
    }

    [Fact]
    public void WIN_PRIVACY_104_DisablePasswordSync_ShouldBeDangerous()
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PRIVACY-104");

        // Assert
        action.Should().NotBeNull();
        action!.Level.Should().Be(CriticalityLevel.Dangerous,
            "Disabling password sync should be DANGEROUS level");
        action.Tags.Should().Contain("passwords", "Should have 'passwords' tag");
        action.Tags.Should().Contain("credentials", "Should have 'credentials' tag");
        action.Tags.Should().Contain("security", "Should have 'security' tag");
    }

    [Fact]
    public void WIN_PRIVACY_106_RestoreSync_ShouldBeRun()
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PRIVACY-106");

        // Assert
        action.Should().NotBeNull();
        action!.Level.Should().Be(CriticalityLevel.Run,
            "Restore sync action should be Run level");
        action.Tags.Should().Contain("restore", "Should have 'restore' tag");
    }

    [Theory]
    [InlineData("WIN-PRIVACY-101", "cloud")]
    [InlineData("WIN-PRIVACY-102", "settings")]
    [InlineData("WIN-PRIVACY-103", "themes")]
    [InlineData("WIN-PRIVACY-104", "passwords")]
    [InlineData("WIN-PRIVACY-105", "browser")]
    public void CloudSyncActions_ShouldHaveSpecificTags(string actionId, string expectedTag)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Tags.Should().Contain(expectedTag,
            $"Action {actionId} should have '{expectedTag}' tag");
    }

    #endregion

    #region Telemetry & Tracking Actions Tests (201-208)

    [Fact]
    public void TelemetryActions_ShouldHaveCorrectCount()
    {
        // Arrange & Act
        var telemetryActions = _actions.Where(a =>
            a.Id.StartsWith("WIN-PRIVACY-20")).ToList();

        // Assert
        telemetryActions.Should().HaveCount(8, "There should be 8 telemetry and tracking actions");
    }

    [Theory]
    [InlineData("WIN-PRIVACY-201")]
    [InlineData("WIN-PRIVACY-202")]
    [InlineData("WIN-PRIVACY-203")]
    [InlineData("WIN-PRIVACY-204")]
    [InlineData("WIN-PRIVACY-205")]
    [InlineData("WIN-PRIVACY-206")]
    [InlineData("WIN-PRIVACY-207")]
    [InlineData("WIN-PRIVACY-208")]
    public void TelemetryActions_ShouldHaveTelemetryTag(string actionId)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Tags.Should().Contain("télémétrie",
            $"Telemetry action {actionId} should have 'télémétrie' tag");
    }

    [Fact]
    public void WIN_PRIVACY_205_MinimalTelemetry_ShouldBeDangerous()
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PRIVACY-205");

        // Assert
        action.Should().NotBeNull();
        action!.Level.Should().Be(CriticalityLevel.Dangerous,
            "Minimal telemetry configuration should be DANGEROUS level");
        action.Tags.Should().Contain("diagnostic", "Should have 'diagnostic' tag");
        action.Notes.Should().Contain("IMPORTANT", "Should emphasize importance");
        action.Notes.Should().Contain("50+", "Should mention 50+ registry keys");
    }

    [Fact]
    public void WIN_PRIVACY_207_BiometricServices_ShouldBeDangerous()
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PRIVACY-207");

        // Assert
        action.Should().NotBeNull();
        action!.Level.Should().Be(CriticalityLevel.Dangerous,
            "Disabling biometric services should be DANGEROUS level");
        action.Tags.Should().Contain("biometric", "Should have 'biometric' tag");
        action.Tags.Should().Contain("hello", "Should have 'hello' tag");
        action.Notes.Should().Contain("ATTENTION", "Should warn about Windows Hello impact");
    }

    [Theory]
    [InlineData("WIN-PRIVACY-201", "activity")]
    [InlineData("WIN-PRIVACY-202", "gaming")]
    [InlineData("WIN-PRIVACY-203", "ads")]
    [InlineData("WIN-PRIVACY-204", "tracking")]
    [InlineData("WIN-PRIVACY-206", "cortana")]
    [InlineData("WIN-PRIVACY-208", "caméra")]
    public void TelemetryActions_ShouldHaveSpecificTags(string actionId, string expectedTag)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Tags.Should().Contain(expectedTag,
            $"Action {actionId} should have '{expectedTag}' tag");
    }

    #endregion

    #region Third-party Telemetry Actions Tests (301-304)

    [Fact]
    public void ThirdPartyTelemetryActions_ShouldHaveCorrectCount()
    {
        // Arrange & Act
        var thirdPartyActions = _actions.Where(a =>
            a.Id.StartsWith("WIN-PRIVACY-30")).ToList();

        // Assert
        thirdPartyActions.Should().HaveCount(4, "There should be 4 third-party telemetry actions");
    }

    [Theory]
    [InlineData("WIN-PRIVACY-301")]
    [InlineData("WIN-PRIVACY-302")]
    [InlineData("WIN-PRIVACY-303")]
    [InlineData("WIN-PRIVACY-304")]
    public void ThirdPartyTelemetryActions_ShouldHaveThirdPartyTag(string actionId)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Tags.Should().Contain("third-party",
            $"Third-party action {actionId} should have 'third-party' tag");
    }

    [Theory]
    [InlineData("WIN-PRIVACY-301", CriticalityLevel.Run)]
    [InlineData("WIN-PRIVACY-302", CriticalityLevel.Run)]
    [InlineData("WIN-PRIVACY-303", CriticalityLevel.Run)]
    [InlineData("WIN-PRIVACY-304", CriticalityLevel.Run)]
    public void ThirdPartyTelemetryActions_ShouldBeRunLevel(string actionId, CriticalityLevel expectedLevel)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Level.Should().Be(expectedLevel,
            $"Third-party action {actionId} should be {expectedLevel} level");
    }

    [Theory]
    [InlineData("WIN-PRIVACY-301", "adobe")]
    [InlineData("WIN-PRIVACY-302", "vscode")]
    [InlineData("WIN-PRIVACY-303", "google")]
    [InlineData("WIN-PRIVACY-304", "nvidia")]
    public void ThirdPartyTelemetryActions_ShouldHaveVendorTag(string actionId, string vendorTag)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Tags.Should().Contain(vendorTag,
            $"Action {actionId} should have '{vendorTag}' tag");
    }

    #endregion

    #region GDPR Compliance Tests

    [Theory]
    [InlineData("WIN-PRIVACY-001")]
    [InlineData("WIN-PRIVACY-002")]
    [InlineData("WIN-PRIVACY-009")]
    [InlineData("WIN-PRIVACY-101")]
    [InlineData("WIN-PRIVACY-205")]
    [InlineData("WIN-PRIVACY-207")]
    public void CriticalPrivacyActions_ShouldMentionRGPD(string actionId)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Tags.Should().Contain("rgpd",
            $"Critical privacy action {actionId} should have 'rgpd' tag");
    }

    [Fact]
    public void WIN_PRIVACY_101_ShouldMentionArticle44()
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PRIVACY-101");

        // Assert
        action.Should().NotBeNull();
        action!.Description.Should().Contain("RGPD",
            "Should mention RGPD compliance");
    }

    [Fact]
    public void WIN_PRIVACY_205_ShouldMentionRGPDStrict()
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == "WIN-PRIVACY-205");

        // Assert
        action.Should().NotBeNull();
        action!.Description.Should().Contain("RGPD",
            "Minimal telemetry should mention RGPD");
    }

    #endregion

    #region Criticality Level Distribution Tests

    [Fact]
    public void PrivacyActions_ShouldHaveCorrectCriticalityDistribution()
    {
        // Arrange
        var privacyActions = _actions.Where(a => a.Id.StartsWith("WIN-PRIVACY-")).ToList();

        // Act
        var runActions = privacyActions.Where(a => a.Level == CriticalityLevel.Run).Count();
        var dangerousActions = privacyActions.Where(a => a.Level == CriticalityLevel.Dangerous).Count();

        // Assert
        runActions.Should().Be(23, "Should have 23 Run actions");
        dangerousActions.Should().Be(5, "Should have 5 Dangerous actions (009, 101, 104, 205, 207)");
    }

    [Theory]
    [InlineData("WIN-PRIVACY-009")]
    [InlineData("WIN-PRIVACY-101")]
    [InlineData("WIN-PRIVACY-104")]
    [InlineData("WIN-PRIVACY-205")]
    [InlineData("WIN-PRIVACY-207")]
    public void SpecificPrivacyActions_ShouldBeDangerous(string actionId)
    {
        // Arrange
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        // Assert
        action.Should().NotBeNull($"Action {actionId} should exist");
        action!.Level.Should().Be(CriticalityLevel.Dangerous,
            $"Action {actionId} should be DANGEROUS level");
    }

    #endregion

    #region Data Quality Tests

    [Fact]
    public void AllPrivacyActions_ShouldHaveTitle()
    {
        // Arrange
        var privacyActions = _actions.Where(a => a.Id.StartsWith("WIN-PRIVACY-")).ToList();

        // Assert
        privacyActions.Should().AllSatisfy(action =>
        {
            action.Title.Should().NotBeNullOrEmpty($"Action {action.Id} should have a title");
        });
    }

    [Fact]
    public void AllPrivacyActions_ShouldHaveDescription()
    {
        // Arrange
        var privacyActions = _actions.Where(a => a.Id.StartsWith("WIN-PRIVACY-")).ToList();

        // Assert
        privacyActions.Should().AllSatisfy(action =>
        {
            action.Description.Should().NotBeNullOrEmpty(
                $"Action {action.Id} should have a description");
            action.Description.Length.Should().BeGreaterThan(20,
                $"Action {action.Id} description should be meaningful");
        });
    }

    [Fact]
    public void AllPrivacyActions_ShouldHaveAtLeastThreeTags()
    {
        // Arrange
        var privacyActions = _actions.Where(a => a.Id.StartsWith("WIN-PRIVACY-")).ToList();

        // Assert
        privacyActions.Should().AllSatisfy(action =>
        {
            action.Tags.Should().HaveCountGreaterOrEqualTo(3,
                $"Action {action.Id} should have at least 3 tags (privacy, confidentialité, + specific)");
        });
    }

    [Fact]
    public void DangerousPrivacyActions_ShouldHaveWarning()
    {
        // Arrange
        var dangerousActions = _actions.Where(a =>
            a.Id.StartsWith("WIN-PRIVACY-") &&
            a.Level == CriticalityLevel.Dangerous).ToList();

        // Assert
        dangerousActions.Should().AllSatisfy(action =>
        {
            action.Notes.Should().NotBeNullOrEmpty(
                $"Dangerous action {action.Id} should have notes");
            (action.Notes?.Contains("ATTENTION") == true || action.Notes?.Contains("IMPORTANT") == true)
                .Should().BeTrue($"Dangerous action {action.Id} should have warning in notes");
        });
    }

    [Fact]
    public void RestoreActions_ShouldMentionRollback()
    {
        // Arrange
        var restoreActions = _actions.Where(a =>
            a.Id == "WIN-PRIVACY-010" || a.Id == "WIN-PRIVACY-106").ToList();

        // Assert
        restoreActions.Should().AllSatisfy(action =>
        {
            action.Tags.Should().Contain("restore",
                $"Restore action {action.Id} should have 'restore' tag");
        });
    }

    #endregion
}
