namespace Swarm.Shared.EventBus.Events;

/// <summary>
/// Event published when a file is selected in the File Explorer for opening.
/// </summary>
/// <param name="FilePath">The full path of the file to open.</param>
public record FileOpenRequestedEvent(string FilePath); 