using Avalonia;
using Avalonia.ReactiveUI;
using System;

namespace App.UI;

internal class Program
{
    // Avalonia configuration — do not remove
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();   // required for ReactiveObject + ReactiveCommand
}
