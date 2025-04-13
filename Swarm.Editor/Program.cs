using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swarm.Lsp.Configuration;
using Swarm.Lsp.ServerManagement;
using Swarm.Lsp.Services;
using Swarm.Editor.ViewModels;
using Swarm.Editor.ViewModels.Panels;
using Swarm.Shared.EventBus;
using Avalonia.ReactiveUI;

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
        var tempLogServices = new ServiceCollection();
        tempLogServices.AddLogging(configure => 
        { 
            configure.SetMinimumLevel(LogLevel.Trace);
            configure.AddDebug(); 
            configure.AddConsole(); 
        });
        var tempLogProvider = tempLogServices.BuildServiceProvider();
        var startupLogger = tempLogProvider.GetService<ILogger<Program>>();
        startupLogger?.LogInformation("--- Application Starting ---");
        
        try
        {
            startupLogger?.LogTrace("Program.Main: Configuring main ServiceCollection...");
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            startupLogger?.LogTrace("Program.Main: Building main ServiceProvider...");
            var serviceProvider = serviceCollection.BuildServiceProvider();
            startupLogger?.LogInformation("Program.Main: Main ServiceProvider built.");

            Console.WriteLine("Program.Main: Calling BuildAvaloniaApp().StartWithClassicDesktopLifetime...");
            BuildAvaloniaApp(serviceProvider)
                .StartWithClassicDesktopLifetime(args);
            Console.WriteLine("Program.Main: StartWithClassicDesktopLifetime returned. (Should not happen unless window closed)");
        }
        catch (Exception e)
        {
            startupLogger?.LogCritical(e, "!!! Program.Main: Unhandled exception during startup !!!");
            Console.WriteLine("!!! Program.Main: Unhandled exception caught !!!");
            Console.WriteLine(e.ToString());
            throw; 
        }
        startupLogger?.LogInformation("--- Application Exiting Normally ---");
        Console.WriteLine("Program.Main: Exiting normally.");
    }


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(IServiceProvider serviceProvider)
    {
        Console.WriteLine("BuildAvaloniaApp: Configuring AppBuilder...");
        return AppBuilder.Configure(() => new App(serviceProvider))
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
    }

    // Service Configuration
    private static void ConfigureServices(IServiceCollection services)
    {
        Console.WriteLine("ConfigureServices: Registering services...");
        services.AddLogging(configure => 
        {
            configure.SetMinimumLevel(LogLevel.Trace);
            configure.AddDebug();
            configure.AddConsole(); 
        });

        services.AddSingleton<ILspConfigProvider, DefaultLspConfigProvider>();
        services.AddSingleton<ILspServerManager, LspServerManager>();
        services.AddSingleton<ILspService, LspService>();

        services.AddSingleton<Swarm.Editor.Models.Services.IApplicationService, Swarm.Editor.Models.Services.ApplicationService>();
        services.AddSingleton<Swarm.Editor.Models.Services.IFileSystemService, Swarm.Editor.Models.Services.FileSystemService>();
        services.AddSingleton<Swarm.Shared.EventBus.IEventBus, Swarm.Shared.EventBus.InMemoryEventBus>();

        services.AddSingleton<Swarm.Editor.ViewModels.Panels.LeftPanelViewModel>();
        services.AddSingleton<Swarm.Editor.ViewModels.Panels.RightPanelViewModel>();
        services.AddSingleton<Swarm.Editor.ViewModels.Panels.ContentPanelViewModel>();
        services.AddSingleton<FileExplorerViewModel>();
        services.AddSingleton<Swarm.Editor.ViewModels.Chat.ChatViewModel>();
        services.AddSingleton<CodeEditorViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        
        Console.WriteLine("ConfigureServices: Service registration complete.");
    }
} 