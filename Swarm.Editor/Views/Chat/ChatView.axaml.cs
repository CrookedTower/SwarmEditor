using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Swarm.Editor.Views.Chat
{
    public partial class ChatView : UserControl
    {
        public ChatView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 