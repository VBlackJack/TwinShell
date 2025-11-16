using System.Text.Json;
using System.Text.RegularExpressions;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.Infrastructure.Services;

/// <summary>
/// Service for interacting with the PowerShell Gallery
/// </summary>
public class PowerShellGalleryService : IPowerShellGalleryService
{
    private readonly ICommandExecutionService _commandExecutionService;
    private readonly IActionService _actionService;

    public PowerShellGalleryService(
        ICommandExecutionService commandExecutionService,
        IActionService actionService)
    {
        _commandExecutionService = commandExecutionService ?? throw new ArgumentNullException(nameof(commandExecutionService));
        _actionService = actionService ?? throw new ArgumentNullException(nameof(actionService));
    }

    public async Task<IEnumerable<PowerShellModule>> SearchModulesAsync(string query, int maxResults = 50)
    {
        // SECURITY: Validate and sanitize query
        if (!ValidateModuleName(query))
        {
            return Enumerable.Empty<PowerShellModule>();
        }

        var command = $@"
            Find-Module -Name '*{EscapeForPowerShell(query)}*' -ErrorAction SilentlyContinue |
            Select-Object -First {maxResults} |
            Select-Object Name, Version, Description, Author, CompanyName, PublishedDate, DownloadCount, ProjectUri, LicenseUri, Tags |
            ConvertTo-Json -Depth 3
        ";

        var result = await _commandExecutionService.ExecuteAsync(
            command,
            Platform.Windows,
            CancellationToken.None,
            timeoutSeconds: 60);

        if (!result.Success || string.IsNullOrWhiteSpace(result.Stdout))
        {
            return Enumerable.Empty<PowerShellModule>();
        }

        try
        {
            var modules = new List<PowerShellModule>();

            // Try to parse as array first
            try
            {
                var jsonModules = JsonSerializer.Deserialize<List<PowerShellGalleryModuleDto>>(result.Stdout);
                if (jsonModules != null)
                {
                    modules.AddRange(jsonModules.Select(MapToModule));
                }
            }
            catch
            {
                // If it's a single object, parse as single module
                var jsonModule = JsonSerializer.Deserialize<PowerShellGalleryModuleDto>(result.Stdout);
                if (jsonModule != null)
                {
                    modules.Add(MapToModule(jsonModule));
                }
            }

            return modules;
        }
        catch
        {
            return Enumerable.Empty<PowerShellModule>();
        }
    }

    public async Task<PowerShellModule?> GetModuleDetailsAsync(string moduleName)
    {
        // SECURITY: Validate module name
        if (!ValidateModuleName(moduleName))
        {
            return null;
        }

        var command = $@"
            Find-Module -Name '{EscapeForPowerShell(moduleName)}' -ErrorAction SilentlyContinue |
            Select-Object Name, Version, Description, Author, CompanyName, PublishedDate, DownloadCount, ProjectUri, LicenseUri, Tags |
            ConvertTo-Json -Depth 3
        ";

        var result = await _commandExecutionService.ExecuteAsync(
            command,
            Platform.Windows,
            CancellationToken.None,
            timeoutSeconds: 30);

        if (!result.Success || string.IsNullOrWhiteSpace(result.Stdout))
        {
            return null;
        }

        try
        {
            var jsonModule = JsonSerializer.Deserialize<PowerShellGalleryModuleDto>(result.Stdout);
            return jsonModule == null ? null : MapToModule(jsonModule);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<PowerShellCommand>> GetModuleCommandsAsync(string moduleName)
    {
        // SECURITY: Validate module name
        if (!ValidateModuleName(moduleName))
        {
            return Enumerable.Empty<PowerShellCommand>();
        }

        var command = $@"
            Import-Module '{EscapeForPowerShell(moduleName)}' -ErrorAction SilentlyContinue
            Get-Command -Module '{EscapeForPowerShell(moduleName)}' -ErrorAction SilentlyContinue |
            Select-Object Name, ModuleName, CommandType |
            ConvertTo-Json -Depth 3
        ";

        var result = await _commandExecutionService.ExecuteAsync(
            command,
            Platform.Windows,
            CancellationToken.None,
            timeoutSeconds: 30);

        if (!result.Success || string.IsNullOrWhiteSpace(result.Stdout))
        {
            return Enumerable.Empty<PowerShellCommand>();
        }

        try
        {
            var commands = new List<PowerShellCommand>();

            try
            {
                var jsonCommands = JsonSerializer.Deserialize<List<PowerShellCommandDto>>(result.Stdout);
                if (jsonCommands != null)
                {
                    commands.AddRange(jsonCommands.Select(c => new PowerShellCommand
                    {
                        Name = c.Name ?? string.Empty,
                        ModuleName = c.ModuleName ?? string.Empty,
                        CommandType = c.CommandType ?? string.Empty
                    }));
                }
            }
            catch
            {
                var jsonCommand = JsonSerializer.Deserialize<PowerShellCommandDto>(result.Stdout);
                if (jsonCommand != null)
                {
                    commands.Add(new PowerShellCommand
                    {
                        Name = jsonCommand.Name ?? string.Empty,
                        ModuleName = jsonCommand.ModuleName ?? string.Empty,
                        CommandType = jsonCommand.CommandType ?? string.Empty
                    });
                }
            }

            return commands;
        }
        catch
        {
            return Enumerable.Empty<PowerShellCommand>();
        }
    }

    public async Task<PowerShellCommand?> GetCommandHelpAsync(string commandName)
    {
        // SECURITY: Validate command name
        if (!ValidateModuleName(commandName))
        {
            return null;
        }

        var command = $@"
            Get-Help '{EscapeForPowerShell(commandName)}' -Full -ErrorAction SilentlyContinue |
            Select-Object @{{Name='Name';Expression={{$_.Name}}}},
                         @{{Name='Synopsis';Expression={{$_.Synopsis}}}},
                         @{{Name='Description';Expression={{($_.Description | Out-String).Trim()}}}},
                         @{{Name='Syntax';Expression={{($_.Syntax | Out-String).Trim()}}}},
                         @{{Name='Parameters';Expression={{$_.Parameters.Parameter | ForEach-Object {{
                            [PSCustomObject]@{{
                                Name = $_.Name
                                Type = $_.Type.Name
                                IsMandatory = $_.Required -eq $true
                                Description = ($_.Description | Out-String).Trim()
                            }}
                         }}}}}},
                         @{{Name='Examples';Expression={{$_.Examples.Example | ForEach-Object {{($_.Code | Out-String).Trim()}}}}}} |
            ConvertTo-Json -Depth 5
        ";

        var result = await _commandExecutionService.ExecuteAsync(
            command,
            Platform.Windows,
            CancellationToken.None,
            timeoutSeconds: 30);

        if (!result.Success || string.IsNullOrWhiteSpace(result.Stdout))
        {
            return null;
        }

        try
        {
            var helpDto = JsonSerializer.Deserialize<PowerShellHelpDto>(result.Stdout);
            if (helpDto == null)
                return null;

            return new PowerShellCommand
            {
                Name = helpDto.Name ?? string.Empty,
                Synopsis = helpDto.Synopsis ?? string.Empty,
                Description = helpDto.Description ?? string.Empty,
                Syntax = new List<string> { helpDto.Syntax ?? string.Empty },
                Parameters = helpDto.Parameters?.Select(p => new PowerShellParameter
                {
                    Name = p.Name ?? string.Empty,
                    Type = p.Type ?? string.Empty,
                    IsMandatory = p.IsMandatory,
                    Description = p.Description ?? string.Empty
                }).ToList() ?? new List<PowerShellParameter>(),
                Examples = helpDto.Examples?.ToList() ?? new List<string>()
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> InstallModuleAsync(string moduleName, string scope = "CurrentUser")
    {
        // SECURITY: Validate module name
        if (!ValidateModuleName(moduleName))
        {
            return false;
        }

        // SECURITY: Validate scope
        if (scope != "CurrentUser" && scope != "AllUsers")
        {
            return false;
        }

        var command = $@"
            Install-Module -Name '{EscapeForPowerShell(moduleName)}' -Scope {scope} -Force -AllowClobber -ErrorAction Stop
        ";

        var result = await _commandExecutionService.ExecuteAsync(
            command,
            Platform.Windows,
            CancellationToken.None,
            timeoutSeconds: 300); // 5 minutes for installation

        return result.Success;
    }

    public async Task<bool> IsModuleInstalledAsync(string moduleName)
    {
        // SECURITY: Validate module name
        if (!ValidateModuleName(moduleName))
        {
            return false;
        }

        var command = $@"
            Get-Module -ListAvailable -Name '{EscapeForPowerShell(moduleName)}' -ErrorAction SilentlyContinue |
            Select-Object -First 1 |
            ConvertTo-Json
        ";

        var result = await _commandExecutionService.ExecuteAsync(
            command,
            Platform.Windows,
            CancellationToken.None,
            timeoutSeconds: 10);

        return result.Success && !string.IsNullOrWhiteSpace(result.Stdout) && result.Stdout.Trim() != "null";
    }

    public async Task<TwinShell.Core.Models.Action> ImportCommandAsActionAsync(PowerShellCommand command)
    {
        // Build command template
        var parameters = command.Parameters
            .Where(p => p.IsMandatory)
            .Select(p => $"-{p.Name} <{p.Name}>")
            .ToList();

        var template = $"{command.Name} {string.Join(" ", parameters)}";

        // Create action
        var action = new TwinShell.Core.Models.Action
        {
            Title = command.Name,
            Description = !string.IsNullOrWhiteSpace(command.Synopsis) ? command.Synopsis : command.Description,
            Category = $"{command.ModuleName} (Gallery)",
            Platform = Platform.Windows,
            Level = CriticalityLevel.Run,
            WindowsCommandTemplate = new CommandTemplate
            {
                Template = template,
                Parameters = command.Parameters.Select(p => new TemplateParameter
                {
                    Name = p.Name,
                    Description = p.Description,
                    IsRequired = p.IsMandatory,
                    DefaultValue = p.DefaultValue
                }).ToList()
            },
            Tags = new List<string> { "PowerShell Gallery", command.ModuleName, command.CommandType },
            Notes = $"Imported from PowerShell module: {command.ModuleName}\n\n{command.Description}",
            IsUserCreated = true
        };

        // Add examples if available
        if (command.Examples.Any())
        {
            action.Examples = command.Examples.Select((example, index) => new CommandExample
            {
                Title = $"Example {index + 1}",
                Command = example,
                Description = $"Example usage of {command.Name}"
            }).ToList();
        }

        return await _actionService.CreateActionAsync(action);
    }

    /// <summary>
    /// Validates module/command name to prevent injection
    /// </summary>
    private static bool ValidateModuleName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // Module names should only contain alphanumeric, dots, dashes, and underscores
        // Max length 100 characters
        if (name.Length > 100)
            return false;

        var validNameRegex = new Regex(@"^[a-zA-Z0-9._-]+$");
        if (!validNameRegex.IsMatch(name))
            return false;

        // Check for dangerous characters that could be used for injection
        var dangerousChars = new[] { ';', '|', '&', '$', '`', '(', ')', '<', '>', '\n', '\r', '"' };
        if (name.Any(c => dangerousChars.Contains(c)))
            return false;

        return true;
    }

    /// <summary>
    /// Escapes string for PowerShell execution
    /// </summary>
    private static string EscapeForPowerShell(string input)
    {
        // Escape single quotes by doubling them
        // This is safe when the string is used within single quotes
        return input.Replace("'", "''");
    }

    /// <summary>
    /// Validates URI to prevent malicious URLs
    /// </summary>
    private static bool ValidateUri(string? uri)
    {
        if (string.IsNullOrEmpty(uri))
            return true; // Null/empty URIs are acceptable

        if (!Uri.TryCreate(uri, UriKind.Absolute, out var result))
            return false;

        // Only allow HTTPS for security
        if (result.Scheme != "https")
            return false;

        // Whitelist of allowed hosts for PowerShell Gallery
        var allowedHosts = new[] { "github.com", "www.github.com", "powershellgallery.com", "www.powershellgallery.com" };
        return allowedHosts.Any(host => result.Host.Equals(host, StringComparison.OrdinalIgnoreCase) ||
                                        result.Host.EndsWith("." + host, StringComparison.OrdinalIgnoreCase));
    }

    private static PowerShellModule MapToModule(PowerShellGalleryModuleDto dto)
    {
        // SECURITY: Validate URIs before storing
        var projectUri = ValidateUri(dto.ProjectUri) ? dto.ProjectUri : null;
        var licenseUri = ValidateUri(dto.LicenseUri) ? dto.LicenseUri : null;

        return new PowerShellModule
        {
            Name = dto.Name ?? string.Empty,
            Version = dto.Version ?? string.Empty,
            Description = dto.Description ?? string.Empty,
            Author = dto.Author ?? string.Empty,
            CompanyName = dto.CompanyName,
            DownloadCount = dto.DownloadCount ?? 0,
            PublishedDate = dto.PublishedDate ?? DateTime.MinValue,
            Tags = dto.Tags?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>(),
            ProjectUri = projectUri,
            LicenseUri = licenseUri
        };
    }

    // DTOs for JSON deserialization
    private class PowerShellGalleryModuleDto
    {
        public string? Name { get; set; }
        public string? Version { get; set; }
        public string? Description { get; set; }
        public string? Author { get; set; }
        public string? CompanyName { get; set; }
        public long? DownloadCount { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string? Tags { get; set; }
        public string? ProjectUri { get; set; }
        public string? LicenseUri { get; set; }
    }

    private class PowerShellCommandDto
    {
        public string? Name { get; set; }
        public string? ModuleName { get; set; }
        public string? CommandType { get; set; }
    }

    private class PowerShellHelpDto
    {
        public string? Name { get; set; }
        public string? Synopsis { get; set; }
        public string? Description { get; set; }
        public string? Syntax { get; set; }
        public List<PowerShellParameterDto>? Parameters { get; set; }
        public List<string>? Examples { get; set; }
    }

    private class PowerShellParameterDto
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public bool IsMandatory { get; set; }
        public string? Description { get; set; }
    }
}
