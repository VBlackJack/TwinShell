using System.Globalization;
using System.Text;

namespace TwinShell.Core.Helpers;

/// <summary>
/// Provides text normalization utilities for search operations.
/// Ensures consistent matching regardless of accents, case, or punctuation variations.
/// </summary>
public static class TextNormalizer
{
    /// <summary>
    /// Normalizes text for search by:
    /// - Converting to lowercase
    /// - Removing diacritics/accents (é→e, à→a, ñ→n, etc.)
    /// - Replacing hyphens and underscores with spaces
    /// - Collapsing multiple spaces into single spaces
    /// - Trimming leading/trailing whitespace
    /// </summary>
    /// <param name="text">Text to normalize</param>
    /// <returns>Normalized text suitable for search matching</returns>
    /// <example>
    /// NormalizeForSearch("Get-Service") → "get service"
    /// NormalizeForSearch("Café_réseau") → "cafe reseau"
    /// NormalizeForSearch("Multi   Space") → "multi space"
    /// </example>
    public static string NormalizeForSearch(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Step 1: Remove diacritics (accents)
        var normalized = RemoveDiacritics(text);

        // Step 2: Convert to lowercase
        normalized = normalized.ToLowerInvariant();

        // Step 3: Replace hyphens, underscores, and dots with spaces
        // This allows "Get-Service", "Get_Service", "Get.Service", and "Get Service" to all match
        normalized = normalized.Replace('-', ' ')
                               .Replace('_', ' ')
                               .Replace('.', ' ');

        // Step 4: Collapse multiple spaces into single space
        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        // Step 5: Trim whitespace
        return normalized.Trim();
    }

    /// <summary>
    /// Removes diacritics (accent marks) from text.
    /// Example: "café" → "cafe", "réseau" → "reseau", "niño" → "nino"
    /// </summary>
    /// <param name="text">Text containing diacritics</param>
    /// <returns>Text with diacritics removed</returns>
    public static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Normalize to FormD (decomposed form) where accents are separate characters
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(normalizedString.Length);

        // Keep only characters that are not "NonSpacingMark" (accents)
        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        // Normalize back to FormC (composed form)
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Splits normalized text into individual search tokens (words).
    /// Useful for multi-word searches where all words should match.
    /// </summary>
    /// <param name="normalizedText">Already normalized text</param>
    /// <returns>Array of individual tokens</returns>
    /// <example>
    /// GetSearchTokens("get active directory user") → ["get", "active", "directory", "user"]
    /// </example>
    public static string[] GetSearchTokens(string normalizedText)
    {
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            return Array.Empty<string>();
        }

        return normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Checks if searchable text contains all tokens from the search query.
    /// All tokens must be present (AND logic), but order doesn't matter.
    /// </summary>
    /// <param name="searchableText">Normalized text to search in</param>
    /// <param name="searchTokens">Normalized search tokens</param>
    /// <returns>True if all tokens are found</returns>
    /// <example>
    /// ContainsAllTokens("active directory user management", ["user", "directory"]) → true
    /// ContainsAllTokens("active directory", ["user", "directory"]) → false (missing "user")
    /// </example>
    public static bool ContainsAllTokens(string searchableText, string[] searchTokens)
    {
        if (string.IsNullOrWhiteSpace(searchableText))
        {
            return false;
        }

        if (searchTokens == null || searchTokens.Length == 0)
        {
            return true;
        }

        // All tokens must be present (AND logic)
        return searchTokens.All(token => searchableText.Contains(token, StringComparison.Ordinal));
    }
}
