namespace Swarm.Shared.EventBus.Events;

/// <summary>
/// Event published when a folder is selected via the 'Open Folder' dialog.
/// </summary>
/// <param name="FolderPath">The full path of the selected folder.</param>
public record FolderSelectedEvent(string FolderPath); 