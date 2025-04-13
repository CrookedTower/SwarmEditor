# Swarm

Swarm is a next-generation, cross-platform code editor and developer assistant platform. It is designed for extensibility, high performance, and deep integration with advanced AI/LLM workflows and a persistent, queryable codebase map ("Hivemind").

## Project Structure

- **Swarm.Editor**  
  The main code editor application (UI, language services, file/project management).  
  Built on C#/.NET and Avalonia for true cross-platform support.

- **Swarm.Agents**  
  The orchestration layer for agentic workflows, LLM integration, and Swarm intelligence.  
  Manages multi-LLM collaboration, tool usage, and agentic task execution.

- **Swarm.Hivemind**  
  The codebase map and query engine.  
  Provides persistent, real-time structural, semantic, and temporal context to the editor and AI components.

- **Swarm.Plugin**  
  The plugin and extension system.  
  Enables third-party and user-defined extensions to add new features, tools, and integrations.

- **Swarm.Shared**  
  Shared utilities, models, and contracts used across all Swarm components.

## Status

- **Current focus:**  
  - Establishing the Swarm.Editor foundation (UI, C# language support)
  - Structuring the codebase for modularity and future Swarm.AI and Hivemind integration

## Vision

Swarm aims to deliver a seamless, intelligent, and extensible development experience, leveraging the power of collaborative AI and a persistent, queryable understanding of your codebase.

---

For more details, see the `memory-bank/` directory for design documents and architectural context.
