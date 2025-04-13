using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;

namespace Swarm.Editor.Models.Services
{
    public interface IApplicationService
    {
        void Exit();
    }

    public class ApplicationService : IApplicationService
    {
        public void Exit()
        {
            // Get the current application lifetime
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
    }
} 