using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Swarm.Editor.Views.Chat
{
    public partial class ChatPanel : UserControl
    {
        public ChatPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 