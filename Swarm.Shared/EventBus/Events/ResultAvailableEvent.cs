namespace Swarm.Shared.EventBus.Events;

/// <summary>
/// Event published by the Agent system when a workflow completes successfully.
/// </summary>
/// <param name="PromptId">The ID of the originating prompt request.</param>
/// <param name="WorkflowId">The ID of the completed workflow.</param>
/// <param name="Result">The final result content.</param>
/// <param name="CompletionTime">Timestamp of completion.</param>
public record ResultAvailableEvent(string PromptId, string WorkflowId, string Result, DateTime CompletionTime); 