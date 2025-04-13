using System.Threading.Tasks;

namespace Swarm.Lsp.ServerManagement
{
    /// <summary>
    /// Manages the installation and retrieval of LSP server executables.
    /// </summary>
    public interface ILspServerManager
    {
        /// <summary>
        /// Ensures the appropriate language server for the given language ID is installed
        /// (downloading and extracting if necessary) and returns the path to its executable.
        /// </summary>
        /// <param name="languageId">The language ID (e.g., "csharp").</param>
        /// <returns>The full path to the server executable, or null if installation/retrieval fails.</returns>
        Task<string?> EnsureServerInstalledAsync(string languageId);
    }
} 