using System.Diagnostics;

namespace PKHeX.TemplateRegen;

public class PGETPickler(string PathPKHeXLegality, string PathRepoPGET)
{
    public void Update()
    {
        AppLogManager.Log("Starting PoGo Enc Tool update...");

        var exe = FindExecutable();
        if (string.IsNullOrEmpty(exe))
        {
            AppLogManager.LogError("PGET executable not found. Please ensure PoGoEncTool is properly cloned and built.");
            return;
        }

        AppLogManager.Log($"Found PGET executable: {Path.GetFileName(exe)}");

        if (!RepoUpdater.UpdateRepo("PoGoEncTool", PathRepoPGET, "main"))
        {
            AppLogManager.LogError("Failed to update PoGoEncTool repository");
            return;
        }

        // Run the tool to generate pickle files
        AppLogManager.Log("Running PGET update process...");

        if (!RunPgetUpdate(exe))
        {
            AppLogManager.LogError("Failed to run PGET update");
            return;
        }

        // Copy generated files
        CopyGeneratedFiles(Path.GetDirectoryName(exe) ?? string.Empty);
    }

    private string FindExecutable()
    {
        // First check if the direct path exists
        if (File.Exists(PathRepoPGET))
        {
            return PathRepoPGET;
        }

        // Search for executable in common locations
        var searchPaths = new[]
        {
            Path.Combine(PathRepoPGET, "PoGoEncTool.WinForms", "bin", "Release", "net8.0-windows"),
            Path.Combine(PathRepoPGET, "PoGoEncTool.WinForms", "bin", "Debug", "net8.0-windows"),
            Path.Combine(PathRepoPGET, "PoGoEncTool.WinForms", "bin", "Release", "net9.0-windows"),
            Path.Combine(PathRepoPGET, "PoGoEncTool.WinForms", "bin", "Debug", "net9.0-windows"),
            Path.Combine(PathRepoPGET, "bin", "Release"),
            Path.Combine(PathRepoPGET, "bin", "Debug"),
            PathRepoPGET
        };

        var exeNames = new[]
        {
            "PoGoEncTool.WinForms.exe",
            "PoGoEncounterTool.exe",
            "pget.exe"
        };

        foreach (var searchPath in searchPaths)
        {
            if (!Directory.Exists(searchPath))
                continue;

            foreach (var exeName in exeNames)
            {
                var fullPath = Path.Combine(searchPath, exeName);
                if (File.Exists(fullPath))
                {
                    AppLogManager.LogDebug($"Found executable at: {fullPath}");
                    return fullPath;
                }
            }

            // Also search with pattern matching
            try
            {
                var files = Directory.GetFiles(searchPath, "*.exe", SearchOption.TopDirectoryOnly)
                    .Where(f => f.Contains("PoGo", StringComparison.OrdinalIgnoreCase) ||
                               f.Contains("pget", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (files.Count > 0)
                {
                    AppLogManager.LogDebug($"Found executable via pattern: {files[0]}");
                    return files[0];
                }
            }
            catch (Exception ex)
            {
                AppLogManager.LogDebug($"Error searching in {searchPath}: {ex.Message}");
            }
        }

        return string.Empty;
    }

    private bool RunPgetUpdate(string exe)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = "--update",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(exe) ?? string.Empty
            };

            AppLogManager.Log($"Executing: {Path.GetFileName(exe)} --update");

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                AppLogManager.LogError("Failed to start PGET process");
                return false;
            }

            // Read output asynchronously
            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    AppLogManager.LogDebug($"PGET: {e.Data}");
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                    AppLogManager.LogWarning($"PGET Error: {e.Data}");
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for completion with timeout
            var completed = process.WaitForExit(300000); // 5 minute timeout

            if (!completed)
            {
                AppLogManager.LogError("PGET process timed out after 5 minutes");
                try { process.Kill(); } catch { }
                return false;
            }

            if (process.ExitCode != 0)
            {
                AppLogManager.LogError($"PGET exited with code {process.ExitCode}");
                if (errorBuilder.Length > 0)
                {
                    AppLogManager.LogError($"PGET errors:\n{errorBuilder}");
                }
                return false;
            }

            AppLogManager.Log("PGET update completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            AppLogManager.LogError($"Exception running PGET: {ex.Message}", ex);
            return false;
        }
    }

    private void CopyGeneratedFiles(string sourceDir)
    {
        AppLogManager.Log("Copying generated pickle files...");

        var dest = Path.Combine(PathPKHeXLegality, "wild");

        if (!Directory.Exists(dest))
        {
            Directory.CreateDirectory(dest);
            AppLogManager.Log($"Created wild directory: {dest}");
        }

        try
        {
            // Find all pkl files in the source directory and subdirectories
            var pklFiles = Directory.GetFiles(sourceDir, "*.pkl", SearchOption.AllDirectories)
                .Where(f => !f.Contains("backup", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (pklFiles.Count == 0)
            {
                AppLogManager.LogWarning("No pickle files found to copy");
                return;
            }

            var copied = 0;
            var failed = 0;
            var totalSize = 0L;

            foreach (var sourceFile in pklFiles)
            {
                try
                {
                    var fileName = Path.GetFileName(sourceFile);
                    var destFile = Path.Combine(dest, fileName);

                    File.Copy(sourceFile, destFile, true);

                    var fileInfo = new FileInfo(sourceFile);
                    totalSize += fileInfo.Length;
                    copied++;

                    AppLogManager.LogDebug($"Copied: {fileName} ({fileInfo.Length:N0} bytes)");
                }
                catch (Exception ex)
                {
                    AppLogManager.LogWarning($"Failed to copy {Path.GetFileName(sourceFile)}: {ex.Message}");
                    failed++;
                }
            }

            var sizeMB = totalSize / (1024.0 * 1024.0);
            AppLogManager.Log($"Copied {copied} pickle files ({sizeMB:F2} MB total) to {dest}");

            if (failed > 0)
            {
                AppLogManager.LogWarning($"Failed to copy {failed} files");
            }

            // List all files in destination for verification
            var destFiles = Directory.GetFiles(dest, "*.pkl");
            AppLogManager.Log($"Total pickle files in destination: {destFiles.Length}");
        }
        catch (Exception ex)
        {
            AppLogManager.LogError($"Error copying pickle files: {ex.Message}", ex);
        }
    }
}
