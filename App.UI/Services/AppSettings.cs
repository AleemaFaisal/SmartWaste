using ReactiveUI;

namespace App.UI.Services;

/// <summary>
/// Global application settings that can be changed at runtime
/// </summary>
public class AppSettings : ReactiveObject
{
    private static AppSettings? _instance;
    private bool _useEntityFramework = true;
    private string _connectionString = string.Empty;

    public static AppSettings Instance => _instance ??= new AppSettings();

    public bool UseEntityFramework
    {
        get => _useEntityFramework;
        set => this.RaiseAndSetIfChanged(ref _useEntityFramework, value);
    }

    public string ConnectionString
    {
        get => _connectionString;
        set => this.RaiseAndSetIfChanged(ref _connectionString, value);
    }

    private AppSettings()
    {
        // Default to Entity Framework
        _useEntityFramework = true;
    }
}
