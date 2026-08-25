using System.Collections.ObjectModel;
using System.Windows.Input;
using TestApp.Client.Models;
using TestApp.Client.Services;

namespace TestApp.Client.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ICommentApiService _apiService;
    private string _name = string.Empty;
    private string _comment = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isLoading;

    public MainViewModel(ICommentApiService apiService)
    {
        _apiService = apiService;
        Comments = new ObservableCollection<CommentModel>();

        SubmitCommand = new RelayCommand(SubmitAsync, CanSubmit);
        RefreshCommand = new RelayCommand(LoadCommentsAsync, () => !IsLoading);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                ((RelayCommand)SubmitCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string Comment
    {
        get => _comment;
        set
        {
            if (SetProperty(ref _comment, value))
            {
                ((RelayCommand)SubmitCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                ((RelayCommand)SubmitCommand).RaiseCanExecuteChanged();
                ((RelayCommand)RefreshCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ObservableCollection<CommentModel> Comments { get; }

    public ICommand SubmitCommand { get; }
    public ICommand RefreshCommand { get; }

    private bool CanSubmit()
    {
        return !IsLoading &&
               !string.IsNullOrWhiteSpace(Name) &&
               !string.IsNullOrWhiteSpace(Comment);
    }

    public async Task LoadCommentsAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading comments...";

        try
        {
            var list = await _apiService.GetCommentsAsync();
            Comments.Clear();
            foreach (var item in list)
            {
                Comments.Add(item);
            }
            StatusMessage = $"Loaded {Comments.Count} comments successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SubmitAsync()
    {
        if (!CanSubmit()) return;

        IsLoading = true;
        StatusMessage = "Submitting comment...";

        var (success, message) = await _apiService.SubmitCommentAsync(Name, Comment);

        if (success)
        {
            StatusMessage = "Comment submitted successfully!";
            Comment = string.Empty; // Clear comment field after successful submit
            await LoadCommentsAsync();
        }
        else
        {
            StatusMessage = $"Submission failed: {message}";
            IsLoading = false;
        }
    }
}
