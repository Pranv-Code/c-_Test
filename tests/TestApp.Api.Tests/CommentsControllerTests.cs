using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestApp.Api.Controllers;
using TestApp.Api.Data;
using TestApp.Api.DTOs;
using TestApp.Api.Models;
using Xunit;

namespace TestApp.Api.Tests;

public class CommentsControllerTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetComments_ReturnsAllComments_OrderedByNewestFirst()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var older = new Comment { Name = "Alice", CommentText = "First post", CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        var newer = new Comment { Name = "Bob", CommentText = "Second post", CreatedAt = DateTime.UtcNow };
        context.Comments.AddRange(older, newer);
        await context.SaveChangesAsync();

        var controller = new CommentsController(context);

        // Act
        var actionResult = await controller.GetComments();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var comments = Assert.IsAssignableFrom<IEnumerable<CommentResponseDto>>(okResult.Value).ToList();

        Assert.Equal(2, comments.Count);
        Assert.Equal("Bob", comments[0].Name); // Newest first
        Assert.Equal("Alice", comments[1].Name);
    }

    [Fact]
    public async Task CreateComment_ValidInput_CreatesRecordAndReturns201Created()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var controller = new CommentsController(context);
        var dto = new CreateCommentDto { Name = "Nuke", Comment = "Hello from user 1" };

        // Act
        var actionResult = await controller.CreateComment(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        var returnedDto = Assert.IsType<CommentResponseDto>(createdResult.Value);

        Assert.Equal("Nuke", returnedDto.Name);
        Assert.Equal("Hello from user 1", returnedDto.Comment);
        Assert.True(returnedDto.Id > 0);

        var dbRecord = await context.Comments.FirstOrDefaultAsync(c => c.Id == returnedDto.Id);
        Assert.NotNull(dbRecord);
        Assert.Equal("Nuke", dbRecord.Name);
    }

    [Theory]
    [InlineData("", "Valid Comment")]
    [InlineData("Valid Name", "")]
    [InlineData("   ", "   ")]
    public async Task CreateComment_EmptyOrWhitespaceInput_Returns400BadRequest(string name, string comment)
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var controller = new CommentsController(context);
        var dto = new CreateCommentDto { Name = name, Comment = comment };

        // Act
        var actionResult = await controller.CreateComment(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }
}
