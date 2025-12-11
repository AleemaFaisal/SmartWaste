using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using App.UI.Views;

namespace App.UI
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Use full login window with MVVM pattern
                var loginViewModel = new ViewModels.LoginViewModel();
                desktop.MainWindow = new LoginWindow
                {
                    DataContext = loginViewModel
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}