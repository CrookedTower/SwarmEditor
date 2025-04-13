using System.Collections.Generic;
using System.Linq;

namespace Swarm.Lsp.Configuration
{
    /// <summary>
    /// Provides a default, hardcoded LSP configuration (initially just OmniSharp).
    /// TODO: Replace with a provider that reads from a settings file (e.g., JSON).
    /// </summary>
    public class DefaultLspConfigProvider : ILspConfigProvider
    {
        private readonly LspConfig _config;

        public DefaultLspConfigProvider()
        {
            // Hardcode OmniSharp v1.39.13 settings (net6.0 version)
            // URLs from https://github.com/OmniSharp/omnisharp-roslyn/releases/tag/v1.39.13
            var omniSharpSettings = new LspServerSettings
            {
                ServerName = "omnisharp",
                Version = "1.39.13",
                LanguageIds = ["csharp"],
                IsEnabled = true,
                // Standard arguments: -s points to solution/folder, --hostPID allows server to exit if host dies.
                Arguments = "-s \"{WorkspaceRoot}\" --hostPID {HostProcessId}",
                PlatformSpecifics = new()
                {
                    // Windows x64
                    { "win-x64", new PlatformDetails(
                        "https://github.com/OmniSharp/omnisharp-roslyn/releases/download/v1.39.13/omnisharp-win-x64-net6.0.zip",
                        "OmniSharp.exe") },
                    // Linux x64
                    { "linux-x64", new PlatformDetails(
                        "https://github.com/OmniSharp/omnisharp-roslyn/releases/download/v1.39.13/omnisharp-linux-x64-net6.0.tar.gz",
                        "run") }, // Assuming 'run' script is the entry point
                    // macOS x64
                    { "osx-x64", new PlatformDetails(
                        "https://github.com/OmniSharp/omnisharp-roslyn/releases/download/v1.39.13/omnisharp-osx-x64-net6.0.tar.gz",
                        "run") }, // Assuming 'run' script is the entry point
                    // macOS Arm64
                    { "osx-arm64", new PlatformDetails(
                        "https://github.com/OmniSharp/omnisharp-roslyn/releases/download/v1.39.13/omnisharp-osx-arm64-net6.0.tar.gz",
                        "run") } // Assuming 'run' script is the entry point
                    // TODO: Add linux-arm64 if needed
                }
            };

            _config = new LspConfig
            {
                Servers = [omniSharpSettings]
                // TODO: Add settings for other servers (clangd, tsserver, etc.) here later
            };
        }

        public LspConfig GetConfig()
        {
            return _config;
        }

        public LspServerSettings? GetSettingsForLanguage(string languageId)
        {
            if (languageId == null) return null;
            var lowerLangId = languageId.ToLowerInvariant();
            // Find the first enabled server configuration that supports the languageId.
            return _config.Servers.FirstOrDefault(s => s.IsEnabled && s.LanguageIds.Contains(lowerLangId));
        }
    }
} 