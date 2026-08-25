using System.Text.Json.Serialization;

namespace TestApp.Client.Models;

public class CommentModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    public string FormattedCreatedAt => CreatedAt.ToLocalTime().ToString("g");
}
