namespace Swarm.Shared.EventBus.Events;

/// <summary>
/// Event published by the Agent system to report workflow progress.
/// </summary>
/// <param name="WorkflowId">The ID of the workflow providing the update.</param>
/// <param name="Message">The progress message.</param>
/// <param name="ProgressPercentage">Progress percentage (0.0 to 1.0).</param>
/// <param name="Timestamp">Timestamp of the update.</param>
public record ProgressUpdateEvent(string WorkflowId, string Message, double ProgressPercentage, DateTime Timestamp); 