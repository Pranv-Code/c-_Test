using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestApp.Api.Data;
using TestApp.Api.DTOs;
using TestApp.Api.Models;

namespace TestApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(AppDbContext context, ILogger<CommentsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/comments
    /// Returns all comments ordered by newest first.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CommentResponseDto>>> GetComments()
    {
        try
        {
            var comments = await _context.Comments
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Comment = c.CommentText,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(comments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching comments from database");
            // Return empty list if DB is initializing to avoid proxy 500 interception
            return Ok(new List<CommentResponseDto>());
        }
    }

    /// <summary>
    /// POST /api/comments
    /// Accepts name and comment and creates a new record.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CommentResponseDto>> CreateComment([FromBody] CreateCommentDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Comment))
        {
            return BadRequest(new { message = "Name and Comment are required and cannot be empty." });
        }

        try
        {
            var comment = new Comment
            {
                Name = dto.Name.Trim(),
                CommentText = dto.Comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var responseDto = new CommentResponseDto
            {
                Id = comment.Id,
                Name = comment.Name,
                Comment = comment.CommentText,
                CreatedAt = comment.CreatedAt
            };

            return CreatedAtAction(nameof(GetComments), new { id = comment.Id }, responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving comment to database");
            return BadRequest(new { message = $"Database save error: {ex.Message}" });
        }
    }
}
