using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Swarm.Editor.ViewModels;
using Swarm.Editor.ViewModels.Chat;
using Swarm.Editor.Models.Services;
using Swarm.Editor.Models.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Swarm.Agents.EventBus;

namespace Swarm.Editor;

// Ensure class is partial to link with source-generated part
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    
    public App()
    {
        InitializeComponent();
        
        // Create service collection and configure services
        var services = new ServiceCollection();
        ConfigureServices(services);
        
        // Build the service provider
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register logging
        services.AddLogging(configure => configure.AddConsole());
        
        // Register core services
        services.AddSingleton<IApplicationService, ApplicationService>();
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<IEventBus, Swarm.Editor.Models.Events.InMemoryEventBus>();
        
        // Register view models
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<FileExplorerViewModel>();
        services.AddTransient<ChatViewModel>();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (_serviceProvider != null)
            {
                // Task 5.3: Resolve dependencies and manually construct MainWindowViewModel
                var appService = _serviceProvider.GetRequiredService<IApplicationService>();
                var fileSystemService = _serviceProvider.GetRequiredService<IFileSystemService>();
                var eventBus = _serviceProvider.GetRequiredService<IEventBus>();
                var chatLogger = _serviceProvider.GetService<ILogger<ChatViewModel>>(); // Use GetService for optional logger
                var fileExplorerVm = _serviceProvider.GetRequiredService<FileExplorerViewModel>();
                var chatVm = _serviceProvider.GetRequiredService<ChatViewModel>();
                
                // Construct the main view model manually with resolved dependencies
                var mainViewModel = new MainWindowViewModel(
                    appService,
                    fileSystemService,
                    eventBus,
                    chatLogger,
                    fileExplorerVm,
                    chatVm
                );
                
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };
            }
            else
            {
                // Fallback if service provider failed to initialize - this should also be updated if necessary
                // For now, leave the fallback as is, but ideally, it should also handle dependencies
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel() // Fallback still uses parameterless constructor
                };
            }
        }
        
        base.OnFrameworkInitializationCompleted();
    }
} 