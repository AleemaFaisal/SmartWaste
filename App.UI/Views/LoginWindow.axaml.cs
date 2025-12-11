using Avalonia.Controls;
using App.UI.ViewModels;

namespace App.UI.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        DataContext = new LoginViewModel();
    }
}
