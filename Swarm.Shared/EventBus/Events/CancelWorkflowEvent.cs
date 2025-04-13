namespace Swarm.Shared.EventBus.Events;

/// <summary>
/// Event published by the Editor to request cancellation of a specific workflow.
/// </summary>
/// <param name="WorkflowId">The ID of the workflow to cancel.</param>
public record CancelWorkflowEvent(string WorkflowId); 