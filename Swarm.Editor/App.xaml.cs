using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Swarm.Editor.ViewModels;
using Swarm.Editor.ViewModels.Chat;
using Swarm.Editor.Models.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Swarm.Shared.EventBus;

namespace Swarm.Editor;

// Ensure class is partial to link with source-generated part
public partial class App : Application
{
    private readonly ILogger<App> _logger;
    private readonly ServiceProvider _serviceProvider;
    
    public App()
    {
        // --- Setup Logging FIRST before DI container ---
        // Create temporary services just for logging during startup
        var loggingServices = new ServiceCollection();
        loggingServices.AddLogging(configure => 
        { 
            configure.SetMinimumLevel(LogLevel.Trace); // Log everything during startup
            configure.AddDebug(); 
            configure.AddConsole(); 
        });
        var loggingProvider = loggingServices.BuildServiceProvider();
        _logger = loggingProvider.GetRequiredService<ILogger<App>>();
        _logger.LogTrace("App Constructor: Starting.");
        // ---------------------------------------------

        InitializeComponent();
        _logger.LogTrace("App Constructor: InitializeComponent() completed.");
        
        // Create service collection and configure services
        var services = new ServiceCollection();
        ConfigureServices(services);
        _logger.LogTrace("App Constructor: ConfigureServices() completed.");
        
        // Build the service provider
        try
        {
            _serviceProvider = services.BuildServiceProvider();
            _logger.LogInformation("App Constructor: ServiceProvider built successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "App Constructor: Failed to build ServiceProvider!");
            // Optionally rethrow or handle critical failure
            throw; 
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        _logger.LogTrace("ConfigureServices: Starting service registration.");
        // Register logging (using the factory configured earlier if possible, or re-add)
        services.AddLogging(configure => 
        {
            configure.SetMinimumLevel(LogLevel.Trace); // Ensure trace level
            configure.AddDebug();
            configure.AddConsole(); 
        });
        
        // Register core services
        services.AddSingleton<IApplicationService, ApplicationService>();
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<IEventBus, Swarm.Shared.EventBus.InMemoryEventBus>();
        _logger.LogDebug("ConfigureServices: Core services registered.");
        
        // Register view models
        services.AddSingleton<Swarm.Editor.ViewModels.Panels.LeftPanelViewModel>();
        services.AddSingleton<Swarm.Editor.ViewModels.Panels.RightPanelViewModel>();
        services.AddSingleton<Swarm.Editor.ViewModels.Panels.ContentPanelViewModel>();
        services.AddSingleton<FileExplorerViewModel>();
        services.AddSingleton<ChatViewModel>();
        services.AddTransient<MainWindowViewModel>();
        _logger.LogDebug("ConfigureServices: ViewModels registered.");
        _logger.LogTrace("ConfigureServices: Service registration completed.");
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _logger.LogTrace("OnFrameworkInitializationCompleted: Starting.");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _logger.LogDebug("OnFrameworkInitializationCompleted: Lifetime is IClassicDesktopStyleApplicationLifetime.");
            // ServiceProvider should not be null here if constructor succeeded
            if (_serviceProvider == null) 
            { 
                _logger.LogError("OnFrameworkInitializationCompleted: ServiceProvider is null!");
                // Handle this critical error - maybe show a simple message box
                return; 
            }
            
            try
            {
                _logger.LogTrace("OnFrameworkInitializationCompleted: Attempting to resolve MainWindowViewModel...");
                var mainViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
                _logger.LogInformation("OnFrameworkInitializationCompleted: MainWindowViewModel resolved successfully.");
                
                _logger.LogTrace("OnFrameworkInitializationCompleted: Creating MainWindow...");
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };
                _logger.LogInformation("OnFrameworkInitializationCompleted: MainWindow created and DataContext set.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "OnFrameworkInitializationCompleted: Failed to resolve/create MainWindow or ViewModel!");
                // Show error to user or handle
                // Example: MessageBox.Show("Critical startup error: " + ex.Message);
                throw; // Rethrow to potentially allow Program.cs handler to catch
            }
        }
        else
        {
            _logger.LogWarning("OnFrameworkInitializationCompleted: Lifetime is not IClassicDesktopStyleApplicationLifetime ({LifetimeType}).", ApplicationLifetime?.GetType().Name);
            // Handle other lifetimes if necessary
        }
        
        _logger.LogTrace("OnFrameworkInitializationCompleted: Calling base method.");
        base.OnFrameworkInitializationCompleted();
        _logger.LogInformation("OnFrameworkInitializationCompleted: Finished.");
    }
} 