using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using TestApp.Client.Models;

namespace TestApp.Client.Services;

public class CommentApiService : ICommentApiService
{
    private readonly HttpClient _httpClient;

    public CommentApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<CommentModel>> GetCommentsAsync()
    {
        try
        {
            var comments = await _httpClient.GetFromJsonAsync<List<CommentModel>>("api/comments");
            return comments ?? new List<CommentModel>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve comments from API: {ex.Message}", ex);
        }
    }

    public async Task<(bool Success, string Message)> SubmitCommentAsync(string name, string commentText)
    {
        try
        {
            var payload = new { name = name.Trim(), comment = commentText.Trim() };
            var response = await _httpClient.PostAsJsonAsync("api/comments", payload);

            if (response.IsSuccessStatusCode)
            {
                return (true, "Comment submitted successfully.");
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            return (false, $"API Error ({response.StatusCode}): {errorBody}");
        }
        catch (Exception ex)
        {
            return (false, $"Connection Error: {ex.Message}");
        }
    }
}
