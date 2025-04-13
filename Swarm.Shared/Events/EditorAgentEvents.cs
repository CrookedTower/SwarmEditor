using System;
using System.Collections.Generic;

namespace Swarm.Shared.Events;

// Request Events (Editor -> Agents)

/// <summary>
/// Event sent by the Editor to request agent processing of a user prompt.
/// </summary>
public record SendPromptEvent(
    string PromptId,
    string Prompt,
    Dictionary<string, object>? AdditionalContext = null
);

/// <summary>
/// Event sent to cancel a running workflow.
/// </summary>
public record CancelWorkflowEvent(string WorkflowId);

// Response Events (Agents -> Editor)

/// <summary>
/// Event sent when a new workflow is initiated in response to a user prompt.
/// </summary>
public record WorkflowStartedEvent(
    string PromptId,
    string WorkflowId,
    DateTime StartTime
);

/// <summary>
/// Event sent to provide interim progress updates during workflow execution.
/// </summary>
public record ProgressUpdateEvent(
    string WorkflowId,
    string Message,
    double ProgressPercentage,
    DateTime Timestamp
);

/// <summary>
/// Event sent when the final result of a workflow is available.
/// </summary>
public record ResultAvailableEvent(
    string PromptId,
    string WorkflowId,
    string Result,
    DateTime CompletionTime
);

/// <summary>
/// Event sent when an error occurs during workflow execution.
/// </summary>
public record WorkflowErrorEvent(
    string PromptId,
    string WorkflowId,
    string ErrorMessage,
    string? StackTrace,
    DateTime ErrorTime
); 