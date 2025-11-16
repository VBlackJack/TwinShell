using System.Net;
using System.Text.RegularExpressions;
using TwinShell.Core.Constants;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.Core.Services;

/// <summary>
/// Service for generating commands from templates
/// </summary>
public class CommandGeneratorService : ICommandGeneratorService
{
    private readonly ILocalizationService _localizationService;

    public CommandGeneratorService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }
    public string GenerateCommand(CommandTemplate template, Dictionary<string, string> parameterValues)
    {
        // Validate input parameters
        if (template == null)
        {
            throw new ArgumentNullException(nameof(template), "Command template cannot be null");
        }

        if (string.IsNullOrWhiteSpace(template.CommandPattern))
        {
            throw new ArgumentException("Command template pattern cannot be null or empty", nameof(template));
        }

        if (parameterValues == null)
        {
            throw new ArgumentNullException(nameof(parameterValues), "Parameter values dictionary cannot be null");
        }

        if (template.Parameters == null)
        {
            // If no parameters defined, return the pattern as-is
            return template.CommandPattern;
        }

        var command = template.CommandPattern;

        foreach (var parameter in template.Parameters)
        {
            if (parameter == null)
            {
                continue; // Skip null parameters
            }

            var value = parameterValues.ContainsKey(parameter.Name)
                ? parameterValues[parameter.Name]
                : parameter.DefaultValue ?? string.Empty;

            // Validate the value according to the parameter type
            if (!ValidateParameterValue(parameter, value, out var validationError))
            {
                throw new InvalidOperationException($"Invalid parameter '{parameter.Name}': {validationError}");
            }

            // Escape the value for shell safety
            var escapedValue = EscapeParameterValue(value, parameter.Type);

            // Replace {parameterName} with actual value
            var placeholder = $"{{{parameter.Name}}}";
            command = command.Replace(placeholder, escapedValue);
        }

        return command;
    }

    public Dictionary<string, string> GetDefaultParameterValues(CommandTemplate template)
    {
        if (template == null)
        {
            throw new ArgumentNullException(nameof(template), "Command template cannot be null");
        }

        var defaults = new Dictionary<string, string>();

        if (template.Parameters == null)
        {
            return defaults; // Return empty dictionary if no parameters
        }

        foreach (var parameter in template.Parameters)
        {
            if (parameter == null)
            {
                continue; // Skip null parameters
            }

            if (!string.IsNullOrEmpty(parameter.DefaultValue))
            {
                defaults[parameter.Name] = parameter.DefaultValue;
            }
        }

        return defaults;
    }

    public bool ValidateParameters(
        CommandTemplate template,
        Dictionary<string, string> parameterValues,
        out List<string> errors)
    {
        errors = new List<string>();

        if (template == null)
        {
            errors.Add("Command template cannot be null");
            return false;
        }

        if (parameterValues == null)
        {
            errors.Add("Parameter values cannot be null");
            return false;
        }

        if (template.Parameters == null || template.Parameters.Count == 0)
        {
            return true; // No parameters to validate
        }

        foreach (var parameter in template.Parameters)
        {
            if (parameter.Required)
            {
                if (!parameterValues.ContainsKey(parameter.Name) ||
                    string.IsNullOrWhiteSpace(parameterValues[parameter.Name]))
                {
                    errors.Add(_localizationService.GetFormattedString(MessageKeys.ValidationParameterRequired, parameter.Label));
                }
            }

            // Type validation
            if (parameterValues.ContainsKey(parameter.Name) &&
                !string.IsNullOrWhiteSpace(parameterValues[parameter.Name]))
            {
                var value = parameterValues[parameter.Name];

                switch (parameter.Type.ToLowerInvariant())
                {
                    case "int":
                    case "integer":
                        if (!int.TryParse(value, out _))
                        {
                            errors.Add(_localizationService.GetFormattedString(MessageKeys.ValidationParameterMustBeInteger, parameter.Label));
                        }
                        break;

                    case "bool":
                    case "boolean":
                        if (!bool.TryParse(value, out _))
                        {
                            errors.Add(_localizationService.GetFormattedString(MessageKeys.ValidationParameterMustBeBoolean, parameter.Label));
                        }
                        break;

                    case "string":
                        if (value.Length > 255)
                        {
                            errors.Add($"Le paramètre '{parameter.Label}' dépasse la longueur maximale de 255 caractères.");
                        }
                        if (ContainsDangerousCharacters(value))
                        {
                            errors.Add($"Le paramètre '{parameter.Label}' contient des caractères interdits.");
                        }
                        break;

                    case "hostname":
                        if (!IsValidHostname(value))
                        {
                            errors.Add($"Le paramètre '{parameter.Label}' n'est pas un nom d'hôte valide.");
                        }
                        break;

                    case "ipaddress":
                        if (!IPAddress.TryParse(value, out _))
                        {
                            errors.Add($"Le paramètre '{parameter.Label}' n'est pas une adresse IP valide.");
                        }
                        break;

                    case "path":
                        if (!IsValidPath(value))
                        {
                            errors.Add($"Le paramètre '{parameter.Label}' n'est pas un chemin valide.");
                        }
                        break;
                }
            }
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Validates a parameter value based on its type
    /// </summary>
    private bool ValidateParameterValue(TemplateParameter parameter, string value, out string error)
    {
        error = string.Empty;

        switch (parameter.Type.ToLower())
        {
            case "hostname":
                if (!IsValidHostname(value))
                {
                    error = "Invalid hostname format";
                    return false;
                }
                break;

            case "ipaddress":
                if (!IPAddress.TryParse(value, out _))
                {
                    error = "Invalid IP address";
                    return false;
                }
                break;

            case "int":
            case "integer":
                if (!int.TryParse(value, out _))
                {
                    error = "Must be an integer";
                    return false;
                }
                break;

            case "path":
                if (!IsValidPath(value))
                {
                    error = "Invalid path format";
                    return false;
                }
                break;

            case "string":
                if (string.IsNullOrWhiteSpace(value) && parameter.Required)
                {
                    error = "Required field";
                    return false;
                }
                if (value.Length > 255)
                {
                    error = "String exceeds maximum length of 255";
                    return false;
                }
                if (ContainsDangerousCharacters(value))
                {
                    error = "Contains dangerous characters";
                    return false;
                }
                break;
        }

        return true;
    }

    /// <summary>
    /// Escapes a parameter value based on its type
    /// </summary>
    private string EscapeParameterValue(string value, string parameterType)
    {
        switch (parameterType.ToLower())
        {
            case "hostname":
            case "ipaddress":
            case "int":
            case "integer":
                // These types are already validated with whitelist, no additional escaping needed
                return value;

            case "path":
            case "string":
            default:
                // Quote for shell safety
                return QuoteForShell(value);
        }
    }

    /// <summary>
    /// Quotes a value for safe shell execution
    /// </summary>
    private static string QuoteForShell(string value)
    {
        // Use single quotes for Bash (safest approach)
        // Escape single quotes by replacing ' with '\''
        return "'" + value.Replace("'", "'\\''") + "'";
    }

    /// <summary>
    /// Validates hostname according to RFC 1123
    /// </summary>
    private bool IsValidHostname(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 255)
            return false;

        // RFC 1123 compliant hostname validation
        var hostnameRegex = new Regex(@"^(?!-)([a-zA-Z0-9-]{1,63}(?<!-)\.)*[a-zA-Z0-9-]{1,63}$");
        return hostnameRegex.IsMatch(value);
    }

    /// <summary>
    /// Validates path format and prevents path traversal
    /// </summary>
    private bool IsValidPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(value);

            // Check for path traversal attempts
            if (fullPath.Contains("..") || fullPath.Contains("~"))
                return false;

            // Verify path is rooted (absolute path)
            return Path.IsPathRooted(fullPath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a value contains dangerous shell characters
    /// </summary>
    private bool ContainsDangerousCharacters(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        // List of dangerous characters that could be used for command injection
        var dangerousChars = new[] { '&', '|', ';', '`', '$', '(', ')', '<', '>', '\n', '\r' };
        return value.Any(c => dangerousChars.Contains(c) || char.IsControl(c));
    }
}
