using System.Text.RegularExpressions;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.Core.Services;

/// <summary>
/// Service for generating commands from templates
/// </summary>
public class CommandGeneratorService : ICommandGeneratorService
{
    public string GenerateCommand(CommandTemplate template, Dictionary<string, string> parameterValues)
    {
        var command = template.CommandPattern;

        foreach (var parameter in template.Parameters)
        {
            var value = parameterValues.ContainsKey(parameter.Name)
                ? parameterValues[parameter.Name]
                : parameter.DefaultValue ?? string.Empty;

            // Replace {parameterName} with actual value
            var placeholder = $"{{{parameter.Name}}}";
            command = command.Replace(placeholder, value);
        }

        return command;
    }

    public Dictionary<string, string> GetDefaultParameterValues(CommandTemplate template)
    {
        var defaults = new Dictionary<string, string>();

        foreach (var parameter in template.Parameters)
        {
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

        foreach (var parameter in template.Parameters)
        {
            if (parameter.Required)
            {
                if (!parameterValues.ContainsKey(parameter.Name) ||
                    string.IsNullOrWhiteSpace(parameterValues[parameter.Name]))
                {
                    errors.Add($"Le paramètre '{parameter.Label}' est requis.");
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
                            errors.Add($"Le paramètre '{parameter.Label}' doit être un nombre entier.");
                        }
                        break;

                    case "bool":
                    case "boolean":
                        if (!bool.TryParse(value, out _))
                        {
                            errors.Add($"Le paramètre '{parameter.Label}' doit être true ou false.");
                        }
                        break;
                }
            }
        }

        return errors.Count == 0;
    }
}
