/*
 * Copyright 2025 Julien Bombled
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Text.Json;
using FluentAssertions;
using Moq;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using TwinShell.Infrastructure.Services;

namespace TwinShell.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for JsonSyncService
/// </summary>
public class JsonSyncServiceTests : IDisposable
{
    private readonly Mock<IActionRepository> _actionRepositoryMock;
    private readonly Mock<IBatchRepository> _batchRepositoryMock;
    private readonly Mock<ICustomCategoryRepository> _categoryRepositoryMock;
    private readonly Mock<ICommandTemplateRepository> _templateRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly JsonSyncService _service;
    private readonly string _testRootPath;

    public JsonSyncServiceTests()
    {
        _actionRepositoryMock = new Mock<IActionRepository>();
        _batchRepositoryMock = new Mock<IBatchRepository>();
        _categoryRepositoryMock = new Mock<ICustomCategoryRepository>();
        _templateRepositoryMock = new Mock<ICommandTemplateRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _service = new JsonSyncService(
            _actionRepositoryMock.Object,
            _batchRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _templateRepositoryMock.Object,
            _unitOfWorkMock.Object);

        _testRootPath = Path.Combine(Path.GetTempPath(), $"JsonSyncServiceTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRootPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRootPath))
        {
            try
            {
                Directory.Delete(_testRootPath, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    #region Export Tests

    [Fact]
    public async Task ExportDataToYamlAsync_CreatesFolderStructure()
    {
        // Arrange
        SetupEmptyRepositories();

        // Act
        var result = await _service.ExportDataToYamlAsync(_testRootPath);

        // Assert
        result.Success.Should().BeTrue();
        Directory.Exists(Path.Combine(_testRootPath, "actions")).Should().BeTrue();
        Directory.Exists(Path.Combine(_testRootPath, "batches")).Should().BeTrue();
        Directory.Exists(Path.Combine(_testRootPath, "templates")).Should().BeTrue();
        Directory.Exists(Path.Combine(_testRootPath, "categories")).Should().BeTrue();
    }

    [Fact]
    public async Task ExportDataToYamlAsync_ExportsCategories()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var categories = new List<CustomCategory>
        {
            new CustomCategory
            {
                Id = Guid.NewGuid().ToString(),
                PublicId = categoryId,
                Name = "Network",
                Description = "Network tools",
                IconKey = "network",
                ColorHex = "#FF5722",
                IsSystemCategory = false,
                DisplayOrder = 1
            }
        };

        _categoryRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);
        SetupOtherEmptyRepositories();

        // Act
        var result = await _service.ExportDataToYamlAsync(_testRootPath);

        // Assert
        result.Success.Should().BeTrue();
        result.CategoriesExported.Should().Be(1);

        var categoryFile = Path.Combine(_testRootPath, "categories", "Network.json");
        File.Exists(categoryFile).Should().BeTrue();

        var json = await File.ReadAllTextAsync(categoryFile);
        json.Should().Contain("\"name\": \"Network\"");
        json.Should().Contain($"\"id\": \"{categoryId}\"");
    }

    [Fact]
    public async Task ExportDataToYamlAsync_ExportsActions_OrganizedByCategory()
    {
        // Arrange
        var actionId = Guid.NewGuid();
        var actions = new List<Core.Models.Action>
        {
            new Core.Models.Action
            {
                Id = Guid.NewGuid().ToString(),
                PublicId = actionId,
                Title = "Ping Host",
                Description = "Ping a network host",
                Category = "Network",
                Platform = Platform.Both,
                Level = CriticalityLevel.Info,
                Tags = new List<string> { "network", "diagnostic" },
                Examples = new List<CommandExample>
                {
                    new CommandExample { Command = "ping localhost", Description = "Ping localhost" }
                },
                WindowsExamples = new List<CommandExample>(),
                LinuxExamples = new List<CommandExample>(),
                Links = new List<ExternalLink>(),
                IsUserCreated = false,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _actionRepositoryMock.Setup(r => r.GetAllWithTemplatesAsync()).ReturnsAsync(actions);
        SetupOtherEmptyRepositoriesExceptActions();

        // Act
        var result = await _service.ExportDataToYamlAsync(_testRootPath);

        // Assert
        result.Success.Should().BeTrue();
        result.ActionsExported.Should().Be(1);

        var actionFile = Path.Combine(_testRootPath, "actions", "Network", "Ping Host.json");
        File.Exists(actionFile).Should().BeTrue();
    }

    [Fact]
    public async Task ExportDataToYamlAsync_ExportsTemplates()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var templates = new List<CommandTemplate>
        {
            new CommandTemplate
            {
                Id = Guid.NewGuid().ToString(),
                PublicId = templateId,
                Name = "SSH Connect",
                Platform = Platform.Both,
                CommandPattern = "ssh {user}@{host}",
                Parameters = new List<TemplateParameter>
                {
                    new TemplateParameter { Name = "user", Label = "Username", Type = "string", Required = true },
                    new TemplateParameter { Name = "host", Label = "Hostname", Type = "hostname", Required = true }
                }
            }
        };

        _templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(templates);
        SetupOtherEmptyRepositoriesExceptTemplates();

        // Act
        var result = await _service.ExportDataToYamlAsync(_testRootPath);

        // Assert
        result.Success.Should().BeTrue();
        result.TemplatesExported.Should().Be(1);

        var templateFile = Path.Combine(_testRootPath, "templates", "SSH Connect.json");
        File.Exists(templateFile).Should().BeTrue();
    }

    [Fact]
    public async Task ExportDataToYamlAsync_ExportsBatches()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var batches = new List<CommandBatch>
        {
            new CommandBatch
            {
                Id = Guid.NewGuid().ToString(),
                PublicId = batchId,
                Name = "System Diagnostics",
                Description = "Run diagnostic commands",
                ExecutionMode = BatchExecutionMode.ContinueOnError,
                Tags = new List<string> { "diagnostic" },
                Commands = new List<BatchCommandItem>
                {
                    new BatchCommandItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        Order = 1,
                        Command = "ipconfig /all",
                        ActionTitle = "Network Info",
                        Platform = Platform.Windows
                    }
                },
                IsUserCreated = true,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _batchRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(batches);
        SetupOtherEmptyRepositoriesExceptBatches();

        // Act
        var result = await _service.ExportDataToYamlAsync(_testRootPath);

        // Assert
        result.Success.Should().BeTrue();
        result.BatchesExported.Should().Be(1);

        var batchFile = Path.Combine(_testRootPath, "batches", "System Diagnostics.json");
        File.Exists(batchFile).Should().BeTrue();
    }

    [Fact]
    public async Task ExportDataToYamlAsync_SanitizesFileNames()
    {
        // Arrange
        var categories = new List<CustomCategory>
        {
            new CustomCategory
            {
                Id = Guid.NewGuid().ToString(),
                PublicId = Guid.NewGuid(),
                Name = "Test/Invalid:Name*",
                Description = "Test",
                IconKey = "test",
                ColorHex = "#000000"
            }
        };

        _categoryRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);
        SetupOtherEmptyRepositories();

        // Act
        var result = await _service.ExportDataToYamlAsync(_testRootPath);

        // Assert
        result.Success.Should().BeTrue();
        result.CategoriesExported.Should().Be(1);

        // File should exist with sanitized name (no invalid characters)
        var files = Directory.GetFiles(Path.Combine(_testRootPath, "categories"), "*.json");
        files.Should().HaveCount(1);
        Path.GetFileName(files[0]).Should().NotContain("/");
        Path.GetFileName(files[0]).Should().NotContain(":");
        Path.GetFileName(files[0]).Should().NotContain("*");
    }

    #endregion

    #region Import Tests

    [Fact]
    public async Task ImportDataFromYamlAsync_ImportsCategories()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        SetupCategoryFile(categoryId, "ImportTest");

        _categoryRepositoryMock.Setup(r => r.GetByPublicIdAsync(categoryId)).ReturnsAsync((CustomCategory?)null);
        _categoryRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<CustomCategory>()))
            .ReturnsAsync((CustomCategory c) => c);
        SetupEmptySubfolders();

        // Act
        var result = await _service.ImportDataFromYamlAsync(_testRootPath);

        // Assert
        result.Success.Should().BeTrue();
        result.CategoriesCreated.Should().Be(1);
        _categoryRepositoryMock.Verify(r => r.CreateAsync(It.Is<CustomCategory>(c => c.PublicId == categoryId)), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task ImportDataFromYamlAsync_UpdatesExistingCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        SetupCategoryFile(categoryId, "UpdatedCategory");

        var existingCategory = new CustomCategory
        {
            Id = Guid.NewGuid().ToString(),
            PublicId = categoryId,
            Name = "OldName",
            IconKey = "folder",
            ColorHex = "#000000"
        };

        _categoryRepositoryMock.Setup(r => r.GetByPublicIdAsync(categoryId)).ReturnsAsync(existingCategory);
        _categoryRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<CustomCategory>())).Returns(Task.CompletedTask);
        SetupEmptySubfolders();

        // Act
        var result = await _service.ImportDataFromYamlAsync(_testRootPath);

        // Assert
        result.Success.Should().BeTrue();
        result.CategoriesUpdated.Should().Be(1);
        _categoryRepositoryMock.Verify(r => r.UpdateAsync(It.Is<CustomCategory>(c => c.Name == "UpdatedCategory")), Times.Once);
    }

    [Fact]
    public async Task ImportDataFromYamlAsync_ImportsActions()
    {
        // Arrange
        var actionId = Guid.NewGuid();
        SetupActionFile(actionId, "TestAction", "TestCategory");

        _actionRepositoryMock.Setup(r => r.GetByPublicIdAsync(actionId)).ReturnsAsync((Core.Models.Action?)null);
        _actionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Core.Models.Action>())).Returns(Task.CompletedTask);
        SetupEmptySubfoldersExceptActions();

        // Act
        var result = await _service.ImportDataFromYamlAsync(_testRootPath);

        // Assert
        result.Success.Should().BeTrue();
        result.ActionsCreated.Should().Be(1);
        _actionRepositoryMock.Verify(r => r.AddAsync(It.Is<Core.Models.Action>(a => a.PublicId == actionId)), Times.Once);
    }

    [Fact]
    public async Task ImportDataFromYamlAsync_ImportsTemplates()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        SetupTemplateFile(templateId, "TestTemplate");

        _templateRepositoryMock.Setup(r => r.GetByPublicIdAsync(templateId)).ReturnsAsync((CommandTemplate?)null);
        _templateRepositoryMock.Setup(r => r.AddAsync(It.IsAny<CommandTemplate>())).Returns(Task.CompletedTask);
        SetupEmptySubfoldersExceptTemplates();

        // Act
        var result = await _service.ImportDataFromYamlAsync(_testRootPath);

        // Assert
        result.Success.Should().BeTrue();
        result.TemplatesCreated.Should().Be(1);
        _templateRepositoryMock.Verify(r => r.AddAsync(It.Is<CommandTemplate>(t => t.PublicId == templateId)), Times.Once);
    }

    [Fact]
    public async Task ImportDataFromYamlAsync_ImportsBatches()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        SetupBatchFile(batchId, "TestBatch");

        _batchRepositoryMock.Setup(r => r.GetByPublicIdAsync(batchId)).ReturnsAsync((CommandBatch?)null);
        _batchRepositoryMock.Setup(r => r.AddAsync(It.IsAny<CommandBatch>())).Returns(Task.CompletedTask);
        SetupEmptySubfoldersExceptBatches();

        // Act
        var result = await _service.ImportDataFromYamlAsync(_testRootPath);

        // Assert
        result.Success.Should().BeTrue();
        result.BatchesCreated.Should().Be(1);
        _batchRepositoryMock.Verify(r => r.AddAsync(It.Is<CommandBatch>(b => b.PublicId == batchId)), Times.Once);
    }

    [Fact]
    public async Task ImportDataFromYamlAsync_HandlesPerFileExceptions_WithWarnings()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        SetupCategoryFile(categoryId, "TestCategory");
        SetupEmptySubfolders();

        // Per-file exceptions are caught and logged as warnings
        _categoryRepositoryMock.Setup(r => r.GetByPublicIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((CustomCategory?)null);
        _categoryRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<CustomCategory>()))
            .Returns(Task.FromException<CustomCategory>(new InvalidOperationException("Database error")));

        // Act
        var result = await _service.ImportDataFromYamlAsync(_testRootPath);

        // Assert
        // The exception is caught at file level, so import succeeds but with warnings
        result.Success.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("Failed to import category"));
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task ImportDataFromYamlAsync_RollsBackOnFatalException()
    {
        // Arrange
        SetupCategoryFile(Guid.NewGuid(), "TestCategory");
        SetupEmptySubfolders();

        // Simulate a fatal exception during commit that triggers rollback
        _categoryRepositoryMock.Setup(r => r.GetByPublicIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((CustomCategory?)null);
        _categoryRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<CustomCategory>()))
            .ReturnsAsync((CustomCategory c) => c);
        _unitOfWorkMock.Setup(u => u.CommitTransactionAsync())
            .ThrowsAsync(new InvalidOperationException("Commit failed"));

        // Act
        var result = await _service.ImportDataFromYamlAsync(_testRootPath);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Import failed"));
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task ImportDataFromYamlAsync_DetectsConflicts_KeepsLocalWhenNewer()
    {
        // Arrange
        var actionId = Guid.NewGuid();
        var localUpdatedAt = DateTime.UtcNow;
        var remoteUpdatedAt = DateTime.UtcNow.AddHours(-1);

        SetupActionFileWithTimestamp(actionId, "ConflictAction", "TestCategory", remoteUpdatedAt);
        SetupEmptySubfoldersExceptActions();

        var existingAction = new Core.Models.Action
        {
            Id = Guid.NewGuid().ToString(),
            PublicId = actionId,
            Title = "LocalAction",
            Description = "Local version",
            Category = "TestCategory",
            Platform = Platform.Windows,
            Level = CriticalityLevel.Info,
            Tags = new List<string>(),
            Examples = new List<CommandExample>(),
            WindowsExamples = new List<CommandExample>(),
            LinuxExamples = new List<CommandExample>(),
            Links = new List<ExternalLink>(),
            UpdatedAt = localUpdatedAt
        };

        _actionRepositoryMock.Setup(r => r.GetByPublicIdAsync(actionId)).ReturnsAsync(existingAction);

        // Act
        var result = await _service.ImportDataFromYamlAsync(_testRootPath);

        // Assert
        result.Success.Should().BeTrue();
        result.ActionsSkipped.Should().Be(1);
        result.Conflicts.Should().HaveCount(1);
        result.Conflicts[0].Resolution.Should().Be(SyncConflictResolution.KeepLocal);
        _actionRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Core.Models.Action>()), Times.Never);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task ImportDataFromYamlAsync_SkipsFilesExceedingSizeLimit()
    {
        // Arrange
        var categoriesPath = Path.Combine(_testRootPath, "categories");
        Directory.CreateDirectory(categoriesPath);

        // Create file larger than 100KB
        var largeContent = new string('x', 150 * 1024);
        await File.WriteAllTextAsync(Path.Combine(categoriesPath, "large.json"), largeContent);
        SetupEmptySubfoldersExceptCategories();

        // Act
        var result = await _service.ImportDataFromYamlAsync(_testRootPath);

        // Assert
        result.Success.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("File too large"));
    }

    [Fact]
    public async Task ImportDataFromYamlAsync_ValidatesJsonSchema_RejectsInvalidFiles()
    {
        // Arrange
        var categoriesPath = Path.Combine(_testRootPath, "categories");
        Directory.CreateDirectory(categoriesPath);

        // Create invalid JSON (missing required 'name' field)
        var invalidJson = @"{""id"": ""00000000-0000-0000-0000-000000000001""}";
        await File.WriteAllTextAsync(Path.Combine(categoriesPath, "invalid.json"), invalidJson);
        SetupEmptySubfoldersExceptCategories();

        // Act
        var result = await _service.ImportDataFromYamlAsync(_testRootPath);

        // Assert
        result.Success.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("Schema validation failed"));
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ValidateFolderAsync_ReturnsFalse_WhenFolderDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testRootPath, "non_existent");

        // Act
        var result = await _service.ValidateFolderAsync(nonExistentPath);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("does not exist"));
    }

    [Fact]
    public async Task ValidateFolderAsync_ReturnsValid_WithValidFiles()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        SetupCategoryFile(categoryId, "ValidCategory");

        // Act
        var result = await _service.ValidateFolderAsync(_testRootPath);

        // Assert
        result.IsValid.Should().BeTrue();
        result.CategoryFilesFound.Should().Be(1);
    }

    [Fact]
    public async Task ValidateFolderAsync_CountsFilesFromAllFolders()
    {
        // Arrange
        SetupCategoryFile(Guid.NewGuid(), "Cat1");
        SetupTemplateFile(Guid.NewGuid(), "Template1");
        SetupActionFile(Guid.NewGuid(), "Action1", "TestCategory");
        SetupBatchFile(Guid.NewGuid(), "Batch1");

        // Act
        var result = await _service.ValidateFolderAsync(_testRootPath);

        // Assert
        result.IsValid.Should().BeTrue();
        result.CategoryFilesFound.Should().Be(1);
        result.TemplateFilesFound.Should().Be(1);
        result.ActionFilesFound.Should().Be(1);
        result.BatchFilesFound.Should().Be(1);
        result.TotalFilesFound.Should().Be(4);
    }

    [Fact]
    public async Task ValidateFolderAsync_WarnsWhenNoFilesFound()
    {
        // Arrange - empty folder
        Directory.CreateDirectory(Path.Combine(_testRootPath, "categories"));
        Directory.CreateDirectory(Path.Combine(_testRootPath, "actions"));
        Directory.CreateDirectory(Path.Combine(_testRootPath, "templates"));
        Directory.CreateDirectory(Path.Combine(_testRootPath, "batches"));

        // Act
        var result = await _service.ValidateFolderAsync(_testRootPath);

        // Assert
        result.Warnings.Should().Contain(w => w.Contains("No JSON files found"));
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public async Task ImportDataFromYamlAsync_BeginsTransaction()
    {
        // Arrange
        SetupEmptyRepositories();
        SetupEmptySubfolders();

        // Act
        await _service.ImportDataFromYamlAsync(_testRootPath);

        // Assert
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task ImportDataFromYamlAsync_CommitsTransaction_OnSuccess()
    {
        // Arrange
        SetupEmptyRepositories();
        SetupEmptySubfolders();

        // Act
        var result = await _service.ImportDataFromYamlAsync(_testRootPath);

        // Assert
        result.Success.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Never);
    }

    #endregion

    #region JSON Schema Validator Tests

    [Fact]
    public void ValidateCategory_ReturnsValid_ForValidJson()
    {
        // Arrange
        var json = @"{
            ""id"": ""00000000-0000-0000-0000-000000000001"",
            ""name"": ""Test Category""
        }";

        // Act
        var result = JsonSchemaValidator.ValidateCategory(json);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateCategory_ReturnsInvalid_ForMissingRequiredField()
    {
        // Arrange
        var json = @"{""id"": ""00000000-0000-0000-0000-000000000001""}";

        // Act
        var result = JsonSchemaValidator.ValidateCategory(json);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateTemplate_ReturnsValid_ForValidJson()
    {
        // Arrange
        var json = @"{
            ""id"": ""00000000-0000-0000-0000-000000000001"",
            ""name"": ""Test Template"",
            ""commandPattern"": ""test {param}""
        }";

        // Act
        var result = JsonSchemaValidator.ValidateTemplate(json);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateAction_ReturnsValid_ForValidJson()
    {
        // Arrange
        var json = @"{
            ""id"": ""00000000-0000-0000-0000-000000000001"",
            ""title"": ""Test Action"",
            ""category"": ""Test""
        }";

        // Act
        var result = JsonSchemaValidator.ValidateAction(json);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateBatch_ReturnsValid_ForValidJson()
    {
        // Arrange
        var json = @"{
            ""id"": ""00000000-0000-0000-0000-000000000001"",
            ""name"": ""Test Batch""
        }";

        // Act
        var result = JsonSchemaValidator.ValidateBatch(json);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateCategory_ReturnsInvalid_ForMalformedJson()
    {
        // Arrange
        var json = "{ invalid json }";

        // Act
        var result = JsonSchemaValidator.ValidateCategory(json);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("parse error"));
    }

    [Fact]
    public void ValidateCategory_RejectsAdditionalProperties()
    {
        // Arrange
        var json = @"{
            ""id"": ""00000000-0000-0000-0000-000000000001"",
            ""name"": ""Test"",
            ""unknownField"": ""value""
        }";

        // Act
        var result = JsonSchemaValidator.ValidateCategory(json);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private void SetupEmptyRepositories()
    {
        _categoryRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CustomCategory>());
        _templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CommandTemplate>());
        _actionRepositoryMock.Setup(r => r.GetAllWithTemplatesAsync()).ReturnsAsync(new List<Core.Models.Action>());
        _batchRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CommandBatch>());
    }

    private void SetupOtherEmptyRepositories()
    {
        _templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CommandTemplate>());
        _actionRepositoryMock.Setup(r => r.GetAllWithTemplatesAsync()).ReturnsAsync(new List<Core.Models.Action>());
        _batchRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CommandBatch>());
    }

    private void SetupOtherEmptyRepositoriesExceptActions()
    {
        _categoryRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CustomCategory>());
        _templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CommandTemplate>());
        _batchRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CommandBatch>());
    }

    private void SetupOtherEmptyRepositoriesExceptTemplates()
    {
        _categoryRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CustomCategory>());
        _actionRepositoryMock.Setup(r => r.GetAllWithTemplatesAsync()).ReturnsAsync(new List<Core.Models.Action>());
        _batchRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CommandBatch>());
    }

    private void SetupOtherEmptyRepositoriesExceptBatches()
    {
        _categoryRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CustomCategory>());
        _templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CommandTemplate>());
        _actionRepositoryMock.Setup(r => r.GetAllWithTemplatesAsync()).ReturnsAsync(new List<Core.Models.Action>());
    }

    private void SetupEmptySubfolders()
    {
        SetupEmptySubfoldersExceptCategories();
        if (!Directory.Exists(Path.Combine(_testRootPath, "categories")))
            Directory.CreateDirectory(Path.Combine(_testRootPath, "categories"));
    }

    private void SetupEmptySubfoldersExceptCategories()
    {
        Directory.CreateDirectory(Path.Combine(_testRootPath, "templates"));
        Directory.CreateDirectory(Path.Combine(_testRootPath, "actions"));
        Directory.CreateDirectory(Path.Combine(_testRootPath, "batches"));
    }

    private void SetupEmptySubfoldersExceptActions()
    {
        Directory.CreateDirectory(Path.Combine(_testRootPath, "categories"));
        Directory.CreateDirectory(Path.Combine(_testRootPath, "templates"));
        Directory.CreateDirectory(Path.Combine(_testRootPath, "batches"));
    }

    private void SetupEmptySubfoldersExceptTemplates()
    {
        Directory.CreateDirectory(Path.Combine(_testRootPath, "categories"));
        Directory.CreateDirectory(Path.Combine(_testRootPath, "actions"));
        Directory.CreateDirectory(Path.Combine(_testRootPath, "batches"));
    }

    private void SetupEmptySubfoldersExceptBatches()
    {
        Directory.CreateDirectory(Path.Combine(_testRootPath, "categories"));
        Directory.CreateDirectory(Path.Combine(_testRootPath, "templates"));
        Directory.CreateDirectory(Path.Combine(_testRootPath, "actions"));
    }

    private void SetupCategoryFile(Guid id, string name)
    {
        var categoriesPath = Path.Combine(_testRootPath, "categories");
        Directory.CreateDirectory(categoriesPath);

        var json = JsonSerializer.Serialize(new
        {
            id = id.ToString(),
            name = name,
            description = "Test description",
            iconKey = "folder",
            colorHex = "#2196F3"
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        File.WriteAllText(Path.Combine(categoriesPath, $"{name}.json"), json);
    }

    private void SetupTemplateFile(Guid id, string name)
    {
        var templatesPath = Path.Combine(_testRootPath, "templates");
        Directory.CreateDirectory(templatesPath);

        var json = JsonSerializer.Serialize(new
        {
            id = id.ToString(),
            name = name,
            platform = "Windows",
            commandPattern = "test {param}"
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        File.WriteAllText(Path.Combine(templatesPath, $"{name}.json"), json);
    }

    private void SetupActionFile(Guid id, string title, string category)
    {
        var actionsPath = Path.Combine(_testRootPath, "actions", category);
        Directory.CreateDirectory(actionsPath);

        var json = JsonSerializer.Serialize(new
        {
            id = id.ToString(),
            title = title,
            description = "Test action",
            category = category,
            platform = "Windows",
            level = "Info"
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        File.WriteAllText(Path.Combine(actionsPath, $"{title}.json"), json);
    }

    private void SetupActionFileWithTimestamp(Guid id, string title, string category, DateTime updatedAt)
    {
        var actionsPath = Path.Combine(_testRootPath, "actions", category);
        Directory.CreateDirectory(actionsPath);

        var json = JsonSerializer.Serialize(new
        {
            id = id.ToString(),
            title = title,
            description = "Test action",
            category = category,
            platform = "Windows",
            level = "Info",
            updatedAt = updatedAt
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        File.WriteAllText(Path.Combine(actionsPath, $"{title}.json"), json);
    }

    private void SetupBatchFile(Guid id, string name)
    {
        var batchesPath = Path.Combine(_testRootPath, "batches");
        Directory.CreateDirectory(batchesPath);

        var json = JsonSerializer.Serialize(new
        {
            id = id.ToString(),
            name = name,
            description = "Test batch",
            executionMode = "StopOnError"
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        File.WriteAllText(Path.Combine(batchesPath, $"{name}.json"), json);
    }

    #endregion
}
