using CommunityToolkit.Mvvm.ComponentModel;
using CharityManagement.Client.Models;
using CharityManagement.Client.Services;

namespace CharityManagement.Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly AuthService _authService;
    private readonly AppConfiguration _configuration;
    private string _currentApiBaseUrl;

    [ObservableProperty]
    private ViewModelBase? currentViewModel;

    public string ApplicationTitle { get; }
    public string ApiBaseUrl => _currentApiBaseUrl;

    public MainWindowViewModel(AuthService authService, AppConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
        _currentApiBaseUrl = (_authService.BaseAddress ?? _configuration.ApiBaseUrl).Trim();
        ApplicationTitle = "Клиент управления благотворительностью";

        ShowLogin();
    }

    private void ShowLogin()
    {
        _authService.Logout();

        CurrentViewModel = new LoginViewModel(_authService, _currentApiBaseUrl, HandleLoginSuccess);
    }

    private void HandleLoginSuccess(AuthSession session, string apiBaseUrl)
    {
        _currentApiBaseUrl = apiBaseUrl.Trim();
        OnPropertyChanged(nameof(ApiBaseUrl));
        CurrentViewModel = new MainMenuViewModel(session, _currentApiBaseUrl, ShowLogin);
    }
}
