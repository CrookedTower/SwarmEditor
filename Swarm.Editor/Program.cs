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
        Console.WriteLine("Program.Main: Starting application...");
        try
        {
            Console.WriteLine("Program.Main: Calling BuildAvaloniaApp().StartWithClassicDesktopLifetime...");
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
            Console.WriteLine("Program.Main: StartWithClassicDesktopLifetime returned. (Should not happen unless window closed)");
        }
        catch (Exception e)
        {
            // Log the exception 
            Console.WriteLine("!!! Program.Main: Unhandled exception caught !!!");
            Console.WriteLine(e.ToString());
            // Optionally rethrow, show a message box, etc.
            throw; 
        }
        Console.WriteLine("Program.Main: Exiting normally.");
    }


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        Console.WriteLine("BuildAvaloniaApp: Configuring AppBuilder...");
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
            // .UseReactiveUI(); // Uncomment if using ReactiveUI
    }
} 