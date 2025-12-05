using FluentAssertions;
using TwinShell.Core.Helpers;

namespace TwinShell.Core.Tests.Helpers;

/// <summary>
/// Tests for TextNormalizer to ensure proper text normalization for search.
/// Covers: accent removal, case normalization, punctuation handling, multi-word tokenization.
/// </summary>
public class TextNormalizerTests
{
    #region NormalizeForSearch Tests

    [Fact]
    public void NormalizeForSearch_WithNull_ReturnsEmptyString()
    {
        // Arrange
        string? input = null;

        // Act
        var result = TextNormalizer.NormalizeForSearch(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void NormalizeForSearch_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var input = "";

        // Act
        var result = TextNormalizer.NormalizeForSearch(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void NormalizeForSearch_WithWhitespace_ReturnsEmptyString()
    {
        // Arrange
        var input = "   \t\n  ";

        // Act
        var result = TextNormalizer.NormalizeForSearch(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void NormalizeForSearch_ConvertsToLowercase()
    {
        // Arrange
        var input = "Get-ADUser";

        // Act
        var result = TextNormalizer.NormalizeForSearch(input);

        // Assert
        result.Should().Be("get aduser");
    }

    [Fact]
    public void NormalizeForSearch_RemovesAccents_French()
    {
        // Arrange
        var input = "Réseau café";

        // Act
        var result = TextNormalizer.NormalizeForSearch(input);

        // Assert
        result.Should().Be("reseau cafe");
    }

    [Fact]
    public void NormalizeForSearch_RemovesAccents_Spanish()
    {
        // Arrange
        var input = "Configuración niño";

        // Act
        var result = TextNormalizer.NormalizeForSearch(input);

        // Assert
        result.Should().Be("configuracion nino");
    }

    [Fact]
    public void NormalizeForSearch_RemovesAccents_German()
    {
        // Arrange
        // Note: German ß (eszett) is not decomposed by Unicode normalization
        // It remains as ß after RemoveDiacritics. Only ü → u is converted.
        var input = "Müller Größe";

        // Act
        var result = TextNormalizer.NormalizeForSearch(input);

        // Assert - ß is preserved, only ü is converted to u
        result.Should().Be("muller große");
    }

    [Fact]
    public void NormalizeForSearch_ReplacesHyphensWithSpaces()
    {
        // Arrange
        var input = "Get-Service";

        // Act
        var result = TextNormalizer.NormalizeForSearch(input);

        // Assert
        result.Should().Be("get service");
    }

    [Fact]
    public void NormalizeForSearch_ReplacesUnderscoresWithSpaces()
    {
        // Arrange
        var input = "Get_Service_Status";

        // Act
        var result = TextNormalizer.NormalizeForSearch(input);

        // Assert
        result.Should().Be("get service status");
    }

    [Fact]
    public void NormalizeForSearch_ReplacesDotsWithSpaces()
    {
        // Arrange
        var input = "System.Management.Automation";

        // Act
        var result = TextNormalizer.NormalizeForSearch(input);

        // Assert
        result.Should().Be("system management automation");
    }

    [Fact]
    public void NormalizeForSearch_CollapsesMultipleSpaces()
    {
        // Arrange
        var input = "Get    Multiple   Spaces";

        // Act
        var result = TextNormalizer.NormalizeForSearch(input);

        // Assert
        result.Should().Be("get multiple spaces");
    }

    [Fact]
    public void NormalizeForSearch_TrimsLeadingAndTrailingSpaces()
    {
        // Arrange
        var input = "   Get-Service   ";

        // Act
        var result = TextNormalizer.NormalizeForSearch(input);

        // Assert
        result.Should().Be("get service");
    }

    [Fact]
    public void NormalizeForSearch_CombinesAllNormalizations()
    {
        // Arrange - Complex real-world example
        var input = "  Get-ADUser_Café--Réseau   ";

        // Act
        var result = TextNormalizer.NormalizeForSearch(input);

        // Assert
        result.Should().Be("get aduser cafe reseau");
    }

    [Fact]
    public void NormalizeForSearch_PowerShellCommand_GetService()
    {
        // Arrange
        var inputs = new[] { "Get-Service", "Get Service", "Get_Service", "get-service", "GET-SERVICE" };

        // Act & Assert - All variations should normalize to the same value
        var expected = "get service";
        foreach (var input in inputs)
        {
            var result = TextNormalizer.NormalizeForSearch(input);
            result.Should().Be(expected, $"'{input}' should normalize to '{expected}'");
        }
    }

    #endregion

    #region RemoveDiacritics Tests

    [Fact]
    public void RemoveDiacritics_WithNull_ReturnsEmptyString()
    {
        // Arrange
        string? input = null;

        // Act
        var result = TextNormalizer.RemoveDiacritics(input!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void RemoveDiacritics_WithNoAccents_ReturnsOriginal()
    {
        // Arrange
        var input = "Hello World";

        // Act
        var result = TextNormalizer.RemoveDiacritics(input);

        // Assert
        result.Should().Be("Hello World");
    }

    [Fact]
    public void RemoveDiacritics_RemovesFrenchAccents()
    {
        // Arrange
        // Input: à â ä é è ê ë ï î ô ù û ü ÿ ç À Â Ä É È Ê Ë Ï Î Ô Ù Û Ü Ÿ Ç (31 chars)
        var input = "àâäéèêëïîôùûüÿçÀÂÄÉÈÊËÏÎÔÙÛÜŸÇ";

        // Act
        var result = TextNormalizer.RemoveDiacritics(input);

        // Assert
        // Output: a a a e e e e i i o u u u y c A A A E E E E I I O U U U Y C (30 chars)
        // Note: Only one 'o' from 'ô', not two
        result.Should().Be("aaaeeeeiiouuuycAAAEEEEIIOUUUYC");
    }

    [Fact]
    public void RemoveDiacritics_RemovesSpanishAccents()
    {
        // Arrange
        var input = "áéíóúñÁÉÍÓÚÑ";

        // Act
        var result = TextNormalizer.RemoveDiacritics(input);

        // Assert
        result.Should().Be("aeiounAEIOUN");
    }

    #endregion

    #region GetSearchTokens Tests

    [Fact]
    public void GetSearchTokens_WithNull_ReturnsEmptyArray()
    {
        // Arrange
        string? input = null;

        // Act
        var result = TextNormalizer.GetSearchTokens(input!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetSearchTokens_WithEmptyString_ReturnsEmptyArray()
    {
        // Arrange
        var input = "";

        // Act
        var result = TextNormalizer.GetSearchTokens(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetSearchTokens_WithSingleWord_ReturnsSingleToken()
    {
        // Arrange
        var input = "service";

        // Act
        var result = TextNormalizer.GetSearchTokens(input);

        // Assert
        result.Should().Equal("service");
    }

    [Fact]
    public void GetSearchTokens_WithMultipleWords_ReturnsMultipleTokens()
    {
        // Arrange
        var input = "get active directory user";

        // Act
        var result = TextNormalizer.GetSearchTokens(input);

        // Assert
        result.Should().Equal("get", "active", "directory", "user");
    }

    [Fact]
    public void GetSearchTokens_WithMultipleSpaces_IgnoresExtraSpaces()
    {
        // Arrange
        var input = "get   multiple   spaces";

        // Act
        var result = TextNormalizer.GetSearchTokens(input);

        // Assert
        result.Should().Equal("get", "multiple", "spaces");
    }

    [Fact]
    public void GetSearchTokens_WithLeadingTrailingSpaces_TrimsSpaces()
    {
        // Arrange
        var input = "  get service  ";

        // Act
        var result = TextNormalizer.GetSearchTokens(input);

        // Assert
        result.Should().Equal("get", "service");
    }

    #endregion

    #region ContainsAllTokens Tests

    [Fact]
    public void ContainsAllTokens_WithNullSearchableText_ReturnsFalse()
    {
        // Arrange
        string? searchableText = null;
        var tokens = new[] { "service" };

        // Act
        var result = TextNormalizer.ContainsAllTokens(searchableText!, tokens);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsAllTokens_WithEmptySearchableText_ReturnsFalse()
    {
        // Arrange
        var searchableText = "";
        var tokens = new[] { "service" };

        // Act
        var result = TextNormalizer.ContainsAllTokens(searchableText, tokens);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsAllTokens_WithNullTokens_ReturnsTrue()
    {
        // Arrange
        var searchableText = "get service status";
        string[]? tokens = null;

        // Act
        var result = TextNormalizer.ContainsAllTokens(searchableText, tokens!);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsAllTokens_WithEmptyTokens_ReturnsTrue()
    {
        // Arrange
        var searchableText = "get service status";
        var tokens = Array.Empty<string>();

        // Act
        var result = TextNormalizer.ContainsAllTokens(searchableText, tokens);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsAllTokens_WithSingleMatchingToken_ReturnsTrue()
    {
        // Arrange
        var searchableText = "get service status";
        var tokens = new[] { "service" };

        // Act
        var result = TextNormalizer.ContainsAllTokens(searchableText, tokens);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsAllTokens_WithSingleNonMatchingToken_ReturnsFalse()
    {
        // Arrange
        var searchableText = "get service status";
        var tokens = new[] { "process" };

        // Act
        var result = TextNormalizer.ContainsAllTokens(searchableText, tokens);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsAllTokens_WithAllMatchingTokens_ReturnsTrue()
    {
        // Arrange
        var searchableText = "get active directory user management";
        var tokens = new[] { "user", "directory", "active" };

        // Act
        var result = TextNormalizer.ContainsAllTokens(searchableText, tokens);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsAllTokens_WithSomeMatchingTokens_ReturnsFalse()
    {
        // Arrange
        var searchableText = "get active directory user management";
        var tokens = new[] { "user", "directory", "process" }; // "process" is missing

        // Act
        var result = TextNormalizer.ContainsAllTokens(searchableText, tokens);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsAllTokens_TokenOrderDoesNotMatter_ReturnsTrue()
    {
        // Arrange
        var searchableText = "active directory user management";
        var tokens = new[] { "user", "active" }; // Different order than in text

        // Act
        var result = TextNormalizer.ContainsAllTokens(searchableText, tokens);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsAllTokens_PartialWordMatch_ReturnsTrue()
    {
        // Arrange - "serv" should match "service"
        var searchableText = "get service status";
        var tokens = new[] { "serv" };

        // Act
        var result = TextNormalizer.ContainsAllTokens(searchableText, tokens);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Integration_SearchForGetService_MatchesAllVariations()
    {
        // Arrange - Simulate searching for "Get-Service" with different query variations
        var commandTitle = "Get-Service";
        var normalizedCommand = TextNormalizer.NormalizeForSearch(commandTitle); // "get service"

        var queries = new[] { "Get-Service", "get service", "GET SERVICE", "Get_Service", "service" };

        // Act & Assert
        foreach (var query in queries)
        {
            var normalizedQuery = TextNormalizer.NormalizeForSearch(query);
            var tokens = TextNormalizer.GetSearchTokens(normalizedQuery);
            var matches = TextNormalizer.ContainsAllTokens(normalizedCommand, tokens);

            matches.Should().BeTrue($"Query '{query}' should match command '{commandTitle}'");
        }
    }

    [Fact]
    public void Integration_SearchWithAccents_MatchesNormalizedText()
    {
        // Arrange - Simulate searching for "réseau" in French
        var commandDescription = "Configuration du réseau local";
        var normalizedDescription = TextNormalizer.NormalizeForSearch(commandDescription); // "configuration du reseau local"

        var queries = new[] { "réseau", "reseau", "RESEAU", "Réseau" };

        // Act & Assert
        foreach (var query in queries)
        {
            var normalizedQuery = TextNormalizer.NormalizeForSearch(query);
            var tokens = TextNormalizer.GetSearchTokens(normalizedQuery);
            var matches = TextNormalizer.ContainsAllTokens(normalizedDescription, tokens);

            matches.Should().BeTrue($"Query '{query}' should match description '{commandDescription}'");
        }
    }

    [Fact]
    public void Integration_MultiWordSearch_RequiresAllWords()
    {
        // Arrange
        var commandDescription = "Get all Active Directory users from domain";
        var normalizedDescription = TextNormalizer.NormalizeForSearch(commandDescription);

        // These should match (all words present in the description)
        // Note: "AD" would NOT match because it's not in the description - it's an acronym
        var matchingQueries = new[] { "active directory", "directory users", "get active", "all users" };

        // These should NOT match (missing words)
        var nonMatchingQueries = new[] { "active firewall", "directory process", "get service" };

        // Act & Assert - Matching queries
        foreach (var query in matchingQueries)
        {
            var normalizedQuery = TextNormalizer.NormalizeForSearch(query);
            var tokens = TextNormalizer.GetSearchTokens(normalizedQuery);
            var matches = TextNormalizer.ContainsAllTokens(normalizedDescription, tokens);

            matches.Should().BeTrue($"Query '{query}' should match description '{commandDescription}'");
        }

        // Act & Assert - Non-matching queries
        foreach (var query in nonMatchingQueries)
        {
            var normalizedQuery = TextNormalizer.NormalizeForSearch(query);
            var tokens = TextNormalizer.GetSearchTokens(normalizedQuery);
            var matches = TextNormalizer.ContainsAllTokens(normalizedDescription, tokens);

            matches.Should().BeFalse($"Query '{query}' should NOT match description '{commandDescription}'");
        }
    }

    #endregion
}
