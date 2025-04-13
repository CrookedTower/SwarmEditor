namespace Swarm.Lsp.Configuration
{
    /// <summary>
    /// Interface for providing LSP configuration.
    /// </summary>
    public interface ILspConfigProvider
    {
        /// <summary>
        /// Gets the current LSP configuration.
        /// </summary>
        LspConfig GetConfig();

        /// <summary>
        /// Gets the settings for a specific language ID, if configured and enabled.
        /// </summary>
        /// <param name="languageId">The language ID (e.g., "csharp").</param>
        /// <returns>The server settings or null if not found or disabled.</returns>
        LspServerSettings? GetSettingsForLanguage(string languageId);
    }
} 