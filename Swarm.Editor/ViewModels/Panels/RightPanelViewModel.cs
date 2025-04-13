using ReactiveUI;

namespace Swarm.Editor.ViewModels.Panels;

/// <summary>
/// ViewModel for the content displayed in the right panel/dock.
/// This will typically be the Swarm Chat/Agents panel.
/// </summary>
public class RightPanelViewModel : ViewModelBase
{
    // TODO: Add properties and commands specific to the right panel (e.g., ChatViewModel content)
    private string? _placeholderText = "Right Panel Content";
    public string? PlaceholderText
    {
        get => _placeholderText;
        set => this.RaiseAndSetIfChanged(ref _placeholderText, value);
    }
} 