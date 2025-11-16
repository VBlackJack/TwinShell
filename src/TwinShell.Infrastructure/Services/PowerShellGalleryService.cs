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

    private static string EscapeForPowerShell(string input)
    {
        return input.Replace("'", "''");
    }

    private static PowerShellModule MapToModule(PowerShellGalleryModuleDto dto)
    {
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
            ProjectUri = dto.ProjectUri,
            LicenseUri = dto.LicenseUri
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
