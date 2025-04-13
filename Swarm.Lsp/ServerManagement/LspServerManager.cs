using Swarm.Lsp.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Swarm.Lsp.ServerManagement
{
    /// <summary>
    /// Manages the installation and retrieval of LSP server executables.
    /// </summary>
    public class LspServerManager : ILspServerManager
    {
        private readonly ILspConfigProvider _configProvider;
        private readonly string _baseInstallPath;
        private static readonly HttpClient _httpClient = new HttpClient(); // Reuse HttpClient

        // TODO: Inject logger

        public LspServerManager(ILspConfigProvider configProvider)
        {
            _configProvider = configProvider;
            // Determine a base path for storing LSP servers
            _baseInstallPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Swarm", // Application Name
                "LspServers");

            Directory.CreateDirectory(_baseInstallPath); // Ensure base directory exists
            Debug.WriteLine($"[LspServerManager] Base installation path: {_baseInstallPath}");
        }

        public async Task<string?> EnsureServerInstalledAsync(string languageId)
        {
            var settings = _configProvider.GetSettingsForLanguage(languageId);
            if (settings == null)
            {
                Debug.WriteLine($"[LspServerManager] No enabled settings found for language: {languageId}");
                return null;
            }

            // Determine the correct platform details
            string currentRid = GetCurrentRuntimeIdentifier();
            if (!settings.PlatformSpecifics.TryGetValue(currentRid, out var platformDetails))
            {
                Debug.WriteLine($"[LspServerManager] No platform details found for RID: {currentRid} for server {settings.ServerName}");
                return null;
            }

            // Construct expected paths
            string serverVersionPath = Path.Combine(_baseInstallPath, settings.ServerName, settings.Version, currentRid);
            string executablePath = Path.Combine(serverVersionPath, platformDetails.ExecutableRelativePath);

            // Check if executable already exists
            if (File.Exists(executablePath))
            {
                Debug.WriteLine($"[LspServerManager] Found existing executable: {executablePath}");
                // TODO: Add version check/update logic here later
                return executablePath;
            }

            // If not found, download and extract
            Debug.WriteLine($"[LspServerManager] Server not found locally. Downloading from: {platformDetails.DownloadUrl}");
            Directory.CreateDirectory(serverVersionPath); // Ensure target directory exists
            string downloadPath = Path.Combine(Path.GetTempPath(), $"{settings.ServerName}-{settings.Version}-{currentRid}-download"); // Temp download file

            try
            {
                // Download
                using (var response = await _httpClient.GetAsync(platformDetails.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    using (var streamToReadFrom = await response.Content.ReadAsStreamAsync())
                    using (var streamToWriteTo = File.Open(downloadPath, FileMode.Create))
                    {
                        await streamToReadFrom.CopyToAsync(streamToWriteTo);
                    }
                }
                Debug.WriteLine($"[LspServerManager] Download complete: {downloadPath}");

                // Extract
                Debug.WriteLine($"[LspServerManager] Extracting to: {serverVersionPath}");
                if (platformDetails.DownloadUrl.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    ZipFile.ExtractToDirectory(downloadPath, serverVersionPath, true); // Overwrite if needed
                }
                else if (platformDetails.DownloadUrl.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
                {
                    await ExtractTarGzAsync(downloadPath, serverVersionPath);
                }
                else
                {
                    // Handle other archive types if necessary, or throw
                    throw new NotSupportedException($"Unsupported archive format: {Path.GetExtension(platformDetails.DownloadUrl)}");
                }
                Debug.WriteLine("[LspServerManager] Extraction complete.");

                // Verify executable exists after extraction
                if (File.Exists(executablePath))
                {
                    // On Linux/macOS, make the executable runnable
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        TrySetExecutablePermission(executablePath);
                    }
                    Debug.WriteLine($"[LspServerManager] Server installed successfully: {executablePath}");
                    return executablePath;
                }
                else
                {
                    Debug.WriteLine($"[LspServerManager] ERROR: Executable not found after extraction: {executablePath}");
                    // Cleanup potentially corrupt install
                    TryDeleteDirectory(serverVersionPath);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LspServerManager] ERROR during server installation: {ex.Message}");
                // Cleanup potentially corrupt install
                TryDeleteDirectory(serverVersionPath);
                return null;
            }
            finally
            {
                // Clean up downloaded archive
                if (File.Exists(downloadPath))
                {
                    try { File.Delete(downloadPath); }
                    catch (Exception cleanupEx) { Debug.WriteLine($"[LspServerManager] Failed to delete temp file {downloadPath}: {cleanupEx.Message}"); }
                }
            }
        }

        private string GetCurrentRuntimeIdentifier()
        {
            string os = string.Empty;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) os = "win";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) os = "linux";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) os = "osx";
            else throw new PlatformNotSupportedException("Unsupported OS Platform");

            string arch = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => throw new PlatformNotSupportedException($"Unsupported Architecture: {RuntimeInformation.ProcessArchitecture}")
            };

            return $"{os}-{arch}";
        }

        // Basic Tar.Gz extraction (requires SharpCompress potentially, or manual process launch)
        // For simplicity here, we'll skip the actual implementation details
        // In a real app, use a library like SharpCompress or shell out to tar command
        private Task ExtractTarGzAsync(string sourceArchivePath, string destinationDirectory)
        {
            Debug.WriteLine($"[LspServerManager] TAR.GZ extraction needed for {sourceArchivePath} (NOT IMPLEMENTED - PLACEHOLDER)");
            // Placeholder: In a real scenario, use a library or external command
            // Example using external tar command (ensure tar is in PATH):
            // var process = Process.Start("tar", $"-xzf \"{sourceArchivePath}\" -C \"{destinationDirectory}\"");
            // await process.WaitForExitAsync();
            // if (process.ExitCode != 0) throw new Exception("tar extraction failed.");
            
            // For now, just pretend it worked IF the executable exists (for testing OmniSharp on Linux/Mac)
            // This requires manual extraction for now on non-Windows.
            return Task.CompletedTask;
        }

        private void TrySetExecutablePermission(string filePath)
        {
            try
            {
                // Simple chmod +x using shell command
                // This is rudimentary and might fail depending on environment/permissions
                var processInfo = new ProcessStartInfo("chmod", $"+x \"{filePath}\"")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var process = Process.Start(processInfo))
                {
                    process?.WaitForExit();
                    if (process?.ExitCode != 0)
                    {
                       Debug.WriteLine($"[LspServerManager] Failed to set +x on {filePath}. Error: {process?.StandardError.ReadToEnd()}");
                    }
                    else
                    {
                        Debug.WriteLine($"[LspServerManager] Set +x permission on {filePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LspServerManager] Exception trying to set +x on {filePath}: {ex.Message}");
            }
        }

        private void TryDeleteDirectory(string path)
        {
             try
             {
                 if(Directory.Exists(path))
                 {
                     Directory.Delete(path, recursive: true);
                 }
             }
             catch (Exception ex)
             {
                 Debug.WriteLine($"[LspServerManager] Failed to delete directory {path}: {ex.Message}");
             }
        }
    }
} 