using CommunityToolkit.Mvvm.ComponentModel;
using Swarm.Editor.Models.Services;
using Swarm.Shared.EventBus;
using Swarm.Editor.ViewModels; // Assuming FileExplorerViewModel is here

namespace Swarm.Editor.ViewModels.Panels;

/// <summary>
/// ViewModel for the content displayed in the left panel/dock.
/// This will typically be the File Explorer.
/// </summary>
public partial class LeftPanelViewModel : ObservableObject
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
    public LeftPanelViewModel() : this(new FileSystemService(), new InMemoryEventBus()) 
    {
        // Design-time instantiation - requires parameterless constructors for services
        // or using mock services. For simplicity, using real ones here.
        // Consider using a proper Design-Time Data approach if needed.
    }
} 