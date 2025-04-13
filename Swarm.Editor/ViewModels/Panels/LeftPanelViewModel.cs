// using CommunityToolkit.Mvvm.ComponentModel; // Remove this
using Swarm.Editor.Models.Services;
using Swarm.Shared.EventBus;
using Swarm.Editor.ViewModels; 
using ReactiveUI; // Add this

namespace Swarm.Editor.ViewModels.Panels;

/// <summary>
/// ViewModel for the content displayed in the left panel/dock.
/// This will typically be the File Explorer.
/// </summary>
// public partial class LeftPanelViewModel : ObservableObject // Change this
public class LeftPanelViewModel : ViewModelBase // Use ViewModelBase
{
    // Expose the specific ViewModel needed by the View
    public FileExplorerViewModel FileExplorer { get; }

    // Remove placeholder if no longer needed or keep for design time
    // [ObservableProperty]
    // private string? _placeholderText = "Left Panel Content"; 

    // Inject services needed by child ViewModels
    public LeftPanelViewModel(IFileSystemService fileSystemService, IEventBus eventBus)
    {
        // Create the FileExplorerViewModel, passing required services
        FileExplorer = new FileExplorerViewModel(fileSystemService, eventBus);
    }
    
    // Parameterless constructor for XAML previewer/design-time support (optional)
    // NOTE: Design-time data context might need adjustment if ViewModelBase changes affect it.
    public LeftPanelViewModel() : this(new FileSystemService(), new InMemoryEventBus()) 
    {
        // Design-time instantiation - requires parameterless constructors for services
        // or using mock services. For simplicity, using real ones here.
        // Consider using a proper Design-Time Data approach if needed.
    }
} 