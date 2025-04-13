using ReactiveUI;

namespace Swarm.Editor.ViewModels.Panels;

/// <summary>
/// ViewModel for the main content area, typically displaying open documents/editors.
/// </summary>
public class ContentPanelViewModel : ViewModelBase
{
    // TODO: Add properties and commands for managing open documents/tabs.
    private string? _placeholderText = "Main Content Area";

    public string? PlaceholderText
    {
        get => _placeholderText;
        set => this.RaiseAndSetIfChanged(ref _placeholderText, value);
    }

    // Example: Might hold a collection of DocumentViewModels
    // public ObservableCollection<DocumentViewModel> OpenDocuments { get; }
} 