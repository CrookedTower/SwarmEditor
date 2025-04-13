using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System;

namespace Swarm.Editor;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            // Log the exception 
            Console.WriteLine("Unhandled exception: " + e.ToString());
            // Optionally rethrow, show a message box, etc.
            throw; 
        }
    }


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
            // .UseReactiveUI(); // Uncomment if using ReactiveUI
} 