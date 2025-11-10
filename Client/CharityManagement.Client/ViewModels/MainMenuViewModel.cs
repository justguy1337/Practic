using System;
using CommunityToolkit.Mvvm.Input;
using CharityManagement.Client.Models;

namespace CharityManagement.Client.ViewModels;

public sealed class MainMenuViewModel : ViewModelBase
{
    private readonly Action _onLogout;

    public AuthSession Session { get; }
    public string ApiBaseUrl { get; }
    public IRelayCommand LogoutCommand { get; }

    public MainMenuViewModel(AuthSession session, string apiBaseUrl, Action onLogout)
    {
        Session = session;
        ApiBaseUrl = apiBaseUrl;
        _onLogout = onLogout;

        LogoutCommand = new RelayCommand(() => _onLogout());
    }
}
