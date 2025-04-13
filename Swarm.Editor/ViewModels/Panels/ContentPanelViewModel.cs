using CommunityToolkit.Mvvm.ComponentModel;

namespace Swarm.Editor.ViewModels.Panels;

/// <summary>
/// ViewModel for the main content area, typically displaying open documents/editors.
/// </summary>
public partial class ContentPanelViewModel : ObservableObject
{
    // TODO: Add properties and commands for managing open documents/tabs.
    [ObservableProperty]
    private string? _placeholderText = "Main Content Area";

    // Example: Might hold a collection of DocumentViewModels
    // public ObservableCollection<DocumentViewModel> OpenDocuments { get; }
} 