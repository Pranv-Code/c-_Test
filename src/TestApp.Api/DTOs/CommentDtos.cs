using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TestApp.Api.DTOs;

public class CreateCommentDto
{
    [Required(ErrorMessage = "Name is required.")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Comment is required.")]
    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;
}

public class CommentResponseDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
