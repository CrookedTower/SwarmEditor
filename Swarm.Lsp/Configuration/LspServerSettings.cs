using System.Collections.Generic;

namespace Swarm.Lsp.Configuration
{
    /// <summary>
    /// Defines the download URL and executable path for a specific platform.
    /// </summary>
    public record PlatformDetails(string DownloadUrl, string ExecutableRelativePath);

    /// <summary>
    /// Configuration settings for a single language server.
    /// </summary>
    public class LspServerSettings
    {
        /// <summary>
        /// A unique name for the server (used for installation directory).
        /// </summary>
        public string ServerName { get; set; } = string.Empty;

        /// <summary>
        /// The specific version of the server.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// List of language identifiers (e.g., "csharp", "python") this server handles.
        /// </summary>
        public List<string> LanguageIds { get; set; } = new List<string>();

        /// <summary>
        /// Indicates whether this server configuration is enabled by the user.
        /// </summary>
        public bool IsEnabled { get; set; } = true; // Default to enabled

        /// <summary>
        /// Dictionary mapping Runtime Identifiers (RID) (e.g., "win-x64", "linux-x64")
        /// to platform-specific download and executable details.
        /// </summary>
        public Dictionary<string, PlatformDetails> PlatformSpecifics { get; set; } = new Dictionary<string, PlatformDetails>();

        /// <summary>
        /// Optional command-line arguments to pass when starting the server.
        /// </summary>
        public string? Arguments { get; set; } // e.g., "-s C:\Path\To\Solution.sln"
    }
} 