using TestApp.Client.Models;

namespace TestApp.Client.Services;

public interface ICommentApiService
{
    Task<List<CommentModel>> GetCommentsAsync();
    Task<(bool Success, string Message)> SubmitCommentAsync(string name, string commentText);
}
