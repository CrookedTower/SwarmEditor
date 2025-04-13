using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Swarm.Editor.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using Swarm.Editor.Views;

namespace Swarm.Editor;

// Ensure class is partial to link with source-generated part
public partial class App : Application
{
    private readonly ILogger<App> _logger;
    private readonly IServiceProvider _services;
    
    public App(IServiceProvider services)
    {
        _services = services;
        
        _logger = _services.GetRequiredService<ILogger<App>>();
        _logger.LogTrace("App Constructor: Starting (using provided IServiceProvider).");

        InitializeComponent();
        _logger.LogTrace("App Constructor: InitializeComponent() completed.");
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
            if (_services == null) 
            { 
                Console.WriteLine("CRITICAL ERROR: OnFrameworkInitializationCompleted: _services (IServiceProvider) is null!");
                return; 
            }
            
            try
            {
                _logger.LogTrace("OnFrameworkInitializationCompleted: Attempting to resolve MainWindowViewModel...");
                var mainViewModel = _services.GetRequiredService<MainWindowViewModel>();
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
                throw;
            }
        }
        else
        {
            _logger.LogWarning("OnFrameworkInitializationCompleted: Lifetime is not IClassicDesktopStyleApplicationLifetime ({LifetimeType}).", ApplicationLifetime?.GetType().Name);
        }
        
        _logger.LogTrace("OnFrameworkInitializationCompleted: Calling base method.");
        base.OnFrameworkInitializationCompleted();
        _logger.LogInformation("OnFrameworkInitializationCompleted: Finished.");
    }
} 