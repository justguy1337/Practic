using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CharityManagement.Client.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
