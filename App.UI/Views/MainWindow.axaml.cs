using Avalonia.Controls;
using App.UI.ViewModels;

namespace App.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
