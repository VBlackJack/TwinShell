using FluentAssertions;
using Moq;
using TwinShell.Core.Enums;
using TwinShell.Core.Models;
using TwinShell.Core.Services;
using TwinShell.Core.Interfaces;

namespace TwinShell.Core.Tests.Services;

public class CommandGeneratorServiceTests
{
    private readonly CommandGeneratorService _service;

    public CommandGeneratorServiceTests()
    {
        var mockLocalizationService = new Mock<ILocalizationService>();
        // Configure mock to return formatted validation messages
        mockLocalizationService
            .Setup(x => x.GetFormattedString(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] args) => $"{key}: {string.Join(", ", args)}");
        _service = new CommandGeneratorService(mockLocalizationService.Object);
    }

    [Fact]
    public void GenerateCommand_WithSingleParameter_ReplacesPlaceholder()
    {
        // Arrange
        var template = new CommandTemplate
        {
            Platform = Platform.Windows,
            CommandPattern = "Get-ADUser -Identity {username}",
            Parameters = new List<TemplateParameter>
            {
                new TemplateParameter
                {
                    Name = "username",
                    Label = "Username",
                    Type = "string",
                    Required = true
                }
            }
        };

        var paramValues = new Dictionary<string, string>
        {
            { "username", "jdupont" }
        };

        // Act
        var result = _service.GenerateCommand(template, paramValues);

        // Assert
        // Note: The service now quotes parameter values for safety
        result.Should().Be("Get-ADUser -Identity 'jdupont'");
    }

    [Fact]
    public void GenerateCommand_WithMultipleParameters_ReplacesAllPlaceholders()
    {
        // Arrange
        var template = new CommandTemplate
        {
            Platform = Platform.Windows,
            CommandPattern = "gpresult /R /S {targetHost} /U {username}",
            Parameters = new List<TemplateParameter>
            {
                new TemplateParameter { Name = "targetHost", Label = "Target Host", Type = "string" },
                new TemplateParameter { Name = "username", Label = "Username", Type = "string" }
            }
        };

        var paramValues = new Dictionary<string, string>
        {
            { "targetHost", "SERVER01" },
            { "username", "admin" }
        };

        // Act
        var result = _service.GenerateCommand(template, paramValues);

        // Assert
        // Note: The service now quotes parameter values for safety
        result.Should().Be("gpresult /R /S 'SERVER01' /U 'admin'");
    }

    [Fact]
    public void GenerateCommand_WithMissingParameter_UsesDefaultValue()
    {
        // Arrange
        var template = new CommandTemplate
        {
            Platform = Platform.Windows,
            CommandPattern = "Get-EventLog -LogName {logName}",
            Parameters = new List<TemplateParameter>
            {
                new TemplateParameter
                {
                    Name = "logName",
                    Label = "Log Name",
                    Type = "string",
                    DefaultValue = "System"
                }
            }
        };

        var paramValues = new Dictionary<string, string>();

        // Act
        var result = _service.GenerateCommand(template, paramValues);

        // Assert
        // Note: The service now quotes parameter values for safety
        result.Should().Be("Get-EventLog -LogName 'System'");
    }

    [Fact]
    public void GetDefaultParameterValues_ReturnsCorrectDefaults()
    {
        // Arrange
        var template = new CommandTemplate
        {
            Parameters = new List<TemplateParameter>
            {
                new TemplateParameter { Name = "param1", DefaultValue = "default1" },
                new TemplateParameter { Name = "param2", DefaultValue = "default2" },
                new TemplateParameter { Name = "param3", DefaultValue = null }
            }
        };

        // Act
        var result = _service.GetDefaultParameterValues(template);

        // Assert
        result.Should().HaveCount(2);
        result["param1"].Should().Be("default1");
        result["param2"].Should().Be("default2");
        result.Should().NotContainKey("param3");
    }

    [Fact]
    public void ValidateParameters_WithRequiredParameterMissing_ReturnsFalse()
    {
        // Arrange
        var template = new CommandTemplate
        {
            Parameters = new List<TemplateParameter>
            {
                new TemplateParameter
                {
                    Name = "username",
                    Label = "Username",
                    Type = "string",
                    Required = true
                }
            }
        };

        var paramValues = new Dictionary<string, string>();

        // Act
        var result = _service.ValidateParameters(template, paramValues, out var errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().HaveCount(1);
        // The mock returns "MessageKey: param1, param2, ..." format
        errors[0].Should().Contain("Username");
    }

    [Fact]
    public void ValidateParameters_WithInvalidIntegerType_ReturnsFalse()
    {
        // Arrange
        var template = new CommandTemplate
        {
            Parameters = new List<TemplateParameter>
            {
                new TemplateParameter
                {
                    Name = "count",
                    Label = "Count",
                    Type = "int",
                    Required = false
                }
            }
        };

        var paramValues = new Dictionary<string, string>
        {
            { "count", "not-a-number" }
        };

        // Act
        var result = _service.ValidateParameters(template, paramValues, out var errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().HaveCount(1);
        // The mock returns "MessageKey: param1, param2, ..." format
        errors[0].Should().Contain("Count");
    }

    [Fact]
    public void ValidateParameters_WithValidParameters_ReturnsTrue()
    {
        // Arrange
        var template = new CommandTemplate
        {
            Parameters = new List<TemplateParameter>
            {
                new TemplateParameter
                {
                    Name = "username",
                    Label = "Username",
                    Type = "string",
                    Required = true
                },
                new TemplateParameter
                {
                    Name = "count",
                    Label = "Count",
                    Type = "int",
                    Required = false
                }
            }
        };

        var paramValues = new Dictionary<string, string>
        {
            { "username", "jdupont" },
            { "count", "10" }
        };

        // Act
        var result = _service.ValidateParameters(template, paramValues, out var errors);

        // Assert
        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }
}
