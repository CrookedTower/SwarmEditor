using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Swarm.Editor.ViewModels;

namespace Swarm.Editor.Views.FileExplorer
{
    public partial class FileExplorerView : UserControl
    {
        public FileExplorerView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 