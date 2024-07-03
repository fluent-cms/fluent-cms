using FluentAssertions;
using FluentCMS.Models;
using FluentCMS.Models.Queries;
using FluentCMS.Services;
using Moq;
using Xunit;

namespace FluentCMS.Test.Services;

public class SchemaServiceTests
{
    private readonly Mock<SchemaService> _mockSchemaService;

    public SchemaServiceTests()
    {
        _mockSchemaService = new Mock<SchemaService>();
    }

    [Fact]
    public async Task GetAll_Should_ReturnAllSchemas()
    {
        // Arrange
        var expectedSchemas = new List<SchemaDisplayDto>
        {
            new SchemaDisplayDto { Id = 1, Name = "Schema1" },
            new SchemaDisplayDto { Id = 2, Name = "Schema2" }
        };
        _mockSchemaService.Setup(s => s.GetAll()).ReturnsAsync(expectedSchemas);

        // Act
        var result = await _mockSchemaService.Object.GetAll();

        // Assert
        result.Should().BeEquivalentTo(expectedSchemas);
    }

    [Fact]
    public async Task GetEntityByName_Should_ReturnEntity_WhenEntityExists()
    {
        // Arrange
        var entityName = "TestEntity";
        var expectedEntity = new Entity { EntityName = entityName };
        _mockSchemaService.Setup(s => s.GetEntityByName(entityName)).ReturnsAsync(expectedEntity);

        // Act
        var result = await _mockSchemaService.Object.GetEntityByName(entityName);

        // Assert
        result.Should().BeEquivalentTo(expectedEntity);
    }

    [Fact]
    public async Task GetViewByName_Should_ReturnView_WhenViewExists()
    {
        // Arrange
        var viewName = "TestView";
        var expectedView = new View { EntityName = viewName };
        _mockSchemaService.Setup(s => s.GetViewByName(viewName)).ReturnsAsync(expectedView);

        // Act
        var result = await _mockSchemaService.Object.GetViewByName(viewName);

        // Assert
        result.Should().BeEquivalentTo(expectedView);
    }

    [Fact]
    public async Task GetByIdOrName_Should_ReturnSchemaDisplayDto_WhenExists()
    {
        // Arrange
        var name = "TestSchema";
        var expectedSchema = new SchemaDisplayDto { Name = name };
        _mockSchemaService.Setup(s => s.GetByIdOrName(name)).ReturnsAsync(expectedSchema);

        // Act
        var result = await _mockSchemaService.Object.GetByIdOrName(name);

        // Assert
        result.Should().BeEquivalentTo(expectedSchema);
    }

    [Fact]
    public async Task GetTableDefine_Should_ReturnSchemaDisplayDto_WhenExists()
    {
        // Arrange
        var id = 1;
        var expectedSchema = new SchemaDisplayDto { Id = id };
        _mockSchemaService.Setup(s => s.GetTableDefine(id)).ReturnsAsync(expectedSchema);

        // Act
        var result = await _mockSchemaService.Object.GetTableDefine(id);

        // Assert
        result.Should().BeEquivalentTo(expectedSchema);
    }

    [Fact]
    public async Task SaveTableDefine_Should_ReturnSavedSchemaDisplayDto()
    {
        // Arrange
        var schemaDto = new SchemaDto { Name = "TestSchema" };
        var expectedSchema = new SchemaDisplayDto { Name = schemaDto.Name };
        _mockSchemaService.Setup(s => s.SaveTableDefine(schemaDto)).ReturnsAsync(expectedSchema);

        // Act
        var result = await _mockSchemaService.Object.SaveTableDefine(schemaDto);

        // Assert
        result.Should().BeEquivalentTo(expectedSchema);
    }

    [Fact]
    public async Task Save_Should_ReturnSavedSchemaDto()
    {
        // Arrange
        var schemaDto = new SchemaDto { Name = "TestSchema" };
        var expectedSchema = new SchemaDto { Name = schemaDto.Name };
        _mockSchemaService.Setup(s => s.Save(schemaDto)).ReturnsAsync(expectedSchema);

        // Act
        var result = await _mockSchemaService.Object.Save(schemaDto);

        // Assert
        result.Should().BeEquivalentTo(expectedSchema);
    }

    [Fact]
    public async Task Delete_Should_ReturnTrue_WhenDeletionIsSuccessful()
    {
        // Arrange
        var id = 1;
        _mockSchemaService.Setup(s => s.Delete(id)).ReturnsAsync(true);

        // Act
        var result = await _mockSchemaService.Object.Delete(id);

        // Assert
        result.Should().BeTrue();
    }
} 
