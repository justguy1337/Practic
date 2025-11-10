using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CharityManagement.Client.Services;

namespace CharityManagement.Client.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly AuthService _authService;
    private readonly Action<Models.AuthSession, string> _onSuccess;
    private readonly string _apiBaseUrl;

    public IAsyncRelayCommand LoginCommand { get; }

    [ObservableProperty]
    private string userNameOrEmail = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    public bool IsFormEnabled => !IsBusy;

    public LoginViewModel(AuthService authService, string apiBaseUrl, Action<Models.AuthSession, string> onSuccess)
    {
        _authService = authService;
        _onSuccess = onSuccess;
        _apiBaseUrl = apiBaseUrl?.Trim() ?? string.Empty;

        LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, CanExecuteLogin);
    }

    partial void OnUserNameOrEmailChanged(string value)
    {
        LoginCommand.NotifyCanExecuteChanged();
    }

    partial void OnPasswordChanged(string value)
    {
        LoginCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        LoginCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsFormEnabled));
    }

    private bool CanExecuteLogin()
    {
        return !IsBusy
               && !string.IsNullOrWhiteSpace(UserNameOrEmail)
               && !string.IsNullOrWhiteSpace(Password)
               && !string.IsNullOrWhiteSpace(_apiBaseUrl);
    }

    private async Task ExecuteLoginAsync()
    {
        if (!CanExecuteLogin())
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var userName = UserNameOrEmail.Trim();
            var result = await _authService.LoginAsync(userName, Password, _apiBaseUrl);
            if (!result.IsSuccess || result.Session is null)
            {
                ErrorMessage = result.Error ?? "Не удалось выполнить авторизацию.";
                return;
            }

            _onSuccess(result.Session, _apiBaseUrl);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Неожиданная ошибка: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
