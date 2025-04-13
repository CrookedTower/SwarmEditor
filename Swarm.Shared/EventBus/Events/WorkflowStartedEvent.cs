namespace Swarm.Shared.EventBus.Events;

/// <summary>
/// Event published by the Agent system when a workflow starts processing a prompt.
/// </summary>
/// <param name="PromptId">The ID of the originating prompt request.</param>
/// <param name="WorkflowId">The ID assigned to the new workflow.</param>
/// <param name="StartTime">Timestamp when the workflow started.</param>
public record WorkflowStartedEvent(string PromptId, string WorkflowId, DateTime StartTime); 