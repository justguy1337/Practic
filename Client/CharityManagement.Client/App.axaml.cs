using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using CharityManagement.Client.Models;
using CharityManagement.Client.Services;
using CharityManagement.Client.ViewModels;
using CharityManagement.Client.Views;

namespace CharityManagement.Client;

public partial class App : Application
{
    private ApiClient? _apiClient;
    private AuthService? _authService;
    private AppConfiguration? _configuration;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            _configuration = AppConfiguration.Load();
            _apiClient = new ApiClient(_configuration);
            _authService = new AuthService(_apiClient);

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(_authService, _configuration),
            };

            desktop.Exit += OnDesktopExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _apiClient?.Dispose();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
