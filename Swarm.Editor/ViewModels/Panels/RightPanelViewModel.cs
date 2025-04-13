using CommunityToolkit.Mvvm.ComponentModel;

namespace Swarm.Editor.ViewModels.Panels;

/// <summary>
/// ViewModel for the content displayed in the right panel/dock.
/// This will typically be the Swarm Chat/Agents panel.
/// </summary>
public partial class RightPanelViewModel : ObservableObject
{
    // TODO: Add properties and commands specific to the right panel (e.g., ChatViewModel content)
    [ObservableProperty]
    private string? _placeholderText = "Right Panel Content";
} 