using Avalonia.Controls;
using Swarm.Editor.Views.CodeEditor; // Changed from Controls to Views
using System.Diagnostics; // For Debug.WriteLine
using Avalonia.Markup.Xaml; // Required for AvaloniaXamlLoader
using Swarm.Editor.ViewModels;
// Use explicit namespace for our custom EventArgs to avoid ambiguity
using CustomEventArgs = Swarm.Editor.Views.CodeEditor.TextChangedEventArgs; // Changed from Controls to Views

namespace Swarm.Editor
{
    public partial class MainWindow : Window
    {
        // Removed the _editorControl field as manual control manipulation is no longer required
        // private SwarmCodeEditor? _editorControl;

        public MainWindow()
        {
            // Call the source-generated method
            InitializeComponent();
            
            // Set the DataContext to a new instance of MainWindowViewModel
            DataContext = new MainWindowViewModel();
            
            // Hook loaded event 
            this.Loaded += MainWindow_Loaded;
        }

        // Remove the manually defined InitializeComponent 
        // private void InitializeComponent()
        // {
        //     AvaloniaXamlLoader.Load(this);
        //     // Hook loaded event *after* loading XAML
        //     this.Loaded += MainWindow_Loaded;
        // }

         private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
         {
             // Removed manual loading and event subscription for the editor control.
             // With MVVM, the CodeEditorViewModel bound within MainWindowViewModel now manages the editor's state.
        }

        // Removed the Editor_TextChanged event handler since it's no longer used.
        // private void Editor_TextChanged(object? sender, CustomEventArgs e)
        // {
        //     Debug.WriteLine($"[MainWindow] Editor Text Changed! New Length: {_editorControl?.Text.Length ?? -1}");
        // }
    }
} 