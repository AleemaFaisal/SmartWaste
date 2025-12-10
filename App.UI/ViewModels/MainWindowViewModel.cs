using App.Core;
using App.Factory;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;

namespace App.UI.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private readonly IService _service;

    public ObservableCollection<User> Users { get; } = new();

    public ReactiveCommand<Unit, Task> LoadUsersCommand { get; }

    public MainWindowViewModel()
    {
        // Toggle EF or SP here
        string conn = "Server=localhost;Database=AppDB;User Id=sa;Password=PASSWORD;TrustServerCertificate=True;"; // change password
        _service = ServiceFactory.Create(useEf: true, connectionString: conn);

        LoadUsersCommand = ReactiveCommand.Create(LoadUsers);

        // Auto-load on startup
        _ = LoadUsers();
    }

    private async Task LoadUsers()
    {
        Users.Clear();
        var list = await _service.GetUsersAsync();

        foreach (var user in list)
            Users.Add(user);
    }
}
