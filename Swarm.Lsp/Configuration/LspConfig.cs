using System.Collections.Generic;

namespace Swarm.Lsp.Configuration
{
    /// <summary>
    /// Represents the overall LSP configuration, containing settings for multiple servers.
    /// </summary>
    public class LspConfig
    {
        public List<LspServerSettings> Servers { get; set; } = new List<LspServerSettings>();
    }
} 