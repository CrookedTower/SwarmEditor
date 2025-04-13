namespace Swarm.Shared.EventBus.Events;

/// <summary>
/// Event published when an error occurs during workflow execution.
/// </summary>
/// <param name="WorkflowId">The ID of the workflow where the error occurred.</param>
/// <param name="TaskId">Optional: The ID of the task that failed.</param>
/// <param name="ErrorMessage">A description of the error.</param>
/// <param name="Exception">Optional: The exception object, if available (may not serialize well across boundaries).</param>
public record WorkflowErrorEvent(string WorkflowId, string? TaskId, string ErrorMessage, Exception? Exception = null); 