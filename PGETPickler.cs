using System.Diagnostics;

namespace PKHeX.TemplateRegen;

public class PGETPickler(string PathPKHeXLegality, string PathRepoPGET)
{
    public void Update()
    {
        AppLogManager.Log("Starting PoGo Enc Tool update...");

        var exe = PathRepoPGET;
        if (!File.Exists(exe) || !exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            // Find the first .exe file in the folder that contains "WinForms"
            // Look in common build output directories first
            var searchPaths = new[]
            {
                Path.Combine(PathRepoPGET, "PoGoEncTool.WinForms", "bin", "Release", "net9.0-windows"),
                Path.Combine(PathRepoPGET, "PoGoEncTool.WinForms", "bin", "Debug", "net9.0-windows"),
                Path.Combine(PathRepoPGET, "PoGoEncTool.WinForms", "bin", "Release", "net8.0-windows"),
                Path.Combine(PathRepoPGET, "PoGoEncTool.WinForms", "bin", "Debug", "net8.0-windows"),
                Path.Combine(PathRepoPGET, "bin", "Release"),
                Path.Combine(PathRepoPGET, "bin", "Debug"),
                PathRepoPGET
            };

            exe = null;
            foreach (var searchPath in searchPaths.Where(Directory.Exists))
            {
                exe = Directory.EnumerateFiles(searchPath, "*.exe", SearchOption.TopDirectoryOnly)
                              .FirstOrDefault(z => z.Contains("WinForms") || z.Contains("PoGo"));
                if (exe != null)
                {
                    AppLogManager.LogDebug($"Found exe in: {searchPath}");
                    break;
                }
            }

            // If not found in common locations, search all subdirectories
            if (exe == null)
            {
                AppLogManager.LogWarning("Exe not found in common locations, searching all subdirectories...");
                exe = Directory.EnumerateFiles(PathRepoPGET, "*.exe", SearchOption.AllDirectories)
                              .Where(f => f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                              .FirstOrDefault(z => z.Contains("WinForms") || z.Contains("PoGoEncTool"));
            }

            if (exe is null)
            {
                AppLogManager.LogError("PGET executable not found");
                AppLogManager.LogError("Please ensure PoGoEncTool is built. Run 'dotnet build' in the repository folder.");

                // List what we did find for debugging
                var dllFiles = Directory.GetFiles(PathRepoPGET, "PoGoEncTool*.dll", SearchOption.AllDirectories)
                                       .Take(5)
                                       .Select(Path.GetFileName);
                if (dllFiles.Any())
                {
                    AppLogManager.LogDebug($"Found DLL files but no EXE: {string.Join(", ", dllFiles)}");
                }
                return;
            }
        }

        AppLogManager.Log($"Found PGET executable: {Path.GetFileName(exe)}");

        // Update the repository first to get the latest data.json
        if (!RepoUpdater.UpdateRepo("PoGoEncTool", PathRepoPGET, "main"))
        {
            AppLogManager.LogError("Failed to update PGET repository");
            return;
        }

        // The data.json must be in the repository's Resources folder
        var dataJsonPath = Path.Combine(PathRepoPGET, "Resources", "data.json");
        if (!File.Exists(dataJsonPath))
        {
            AppLogManager.LogError($"data.json not found at: {dataJsonPath}");
            AppLogManager.LogError("This file is required for PGET to generate pickle files");
            AppLogManager.LogError("Please ensure the PoGoEncTool repository is properly cloned");
            return;
        }

        // Verify data.json is not empty and is valid
        var dataJsonInfo = new FileInfo(dataJsonPath);
        AppLogManager.Log($"Repository data.json - Last modified: {dataJsonInfo.LastWriteTime}, Size: {dataJsonInfo.Length:N0} bytes");

        if (dataJsonInfo.Length == 0)
        {
            AppLogManager.LogError("data.json in repository is empty!");
            AppLogManager.LogError("The repository may not have been properly cloned or updated");
            return;
        }

        // Read first few characters to verify it's valid JSON
        try
        {
            var jsonContent = File.ReadAllText(dataJsonPath);
            if (string.IsNullOrWhiteSpace(jsonContent) || jsonContent.Length < 10)
            {
                AppLogManager.LogError("data.json appears to be invalid or empty");
                return;
            }
            AppLogManager.LogDebug($"data.json starts with: {jsonContent.Substring(0, Math.Min(50, jsonContent.Length))}...");
        }
        catch (Exception ex)
        {
            AppLogManager.LogError($"Failed to read data.json: {ex.Message}");
            return;
        }

        // IMPORTANT: Copy data.json to the same directory as the exe
        var exeDir = Path.GetDirectoryName(exe) ?? string.Empty;
        var exeDataJson = Path.Combine(exeDir, "data.json");

        try
        {
            AppLogManager.Log($"Copying data.json from repository to exe directory...");
            AppLogManager.LogDebug($"Source: {dataJsonPath}");
            AppLogManager.LogDebug($"Destination: {exeDataJson}");

            // Always overwrite to ensure we have the latest version
            File.Copy(dataJsonPath, exeDataJson, true);
            AppLogManager.Log($"Successfully copied data.json ({dataJsonInfo.Length:N0} bytes) to exe directory");

            // Verify the copy
            var copiedInfo = new FileInfo(exeDataJson);
            if (copiedInfo.Length != dataJsonInfo.Length)
            {
                AppLogManager.LogError($"Copy verification failed! Source: {dataJsonInfo.Length} bytes, Destination: {copiedInfo.Length} bytes");
                return;
            }
            else
            {
                AppLogManager.Log("data.json copy verified successfully");
            }
        }
        catch (Exception ex)
        {
            AppLogManager.LogError($"Failed to copy data.json to exe directory: {ex.Message}");
            AppLogManager.LogError("PGET requires data.json to be in the same directory as the executable");
            return;
        }

        // Start the executable with --update passed as arg
        AppLogManager.Log("Running PGET with --update argument...");

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

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            AppLogManager.LogError($"Failed to start {exe} executable");
            return;
        }

        // Read output for logging purposes but don't fail on errors
        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                AppLogManager.LogDebug($"PGET: {e.Data}");
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                AppLogManager.LogWarning($"PGET Error: {e.Data}");
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait for the process to complete (matching original behavior)
        process.WaitForExit();

        var exitCode = process.ExitCode;
        if (exitCode != 0)
        {
            AppLogManager.LogWarning($"PGET exited with code {exitCode}, but continuing to check for generated files...");
        }

        // Get all the created .pkl files then copy them to the destination folder
        // This matches the original behavior - search ALL subdirectories from the exe location
        var dest = Path.Combine(PathPKHeXLegality, "wild");

        if (!Directory.Exists(dest))
        {
            Directory.CreateDirectory(dest);
            AppLogManager.Log($"Created wild directory: {dest}");
        }

        var searchDir = Path.GetDirectoryName(exe) ?? string.Empty;
        AppLogManager.Log($"Searching for .pkl files in: {searchDir}");

        // Also check the repository root directory in case files are generated there
        var additionalSearchDirs = new[] { PathRepoPGET, Path.Combine(PathRepoPGET, "Resources") };

        var files = new List<string>();

        // Search in exe directory first
        if (Directory.Exists(searchDir))
        {
            files.AddRange(Directory.EnumerateFiles(searchDir, "*.pkl", SearchOption.AllDirectories)
                                   .Where(f => !f.Contains("backup", StringComparison.OrdinalIgnoreCase)));
        }

        // Also search in additional directories
        foreach (var dir in additionalSearchDirs.Where(d => Directory.Exists(d) && d != searchDir))
        {
            files.AddRange(Directory.EnumerateFiles(dir, "*.pkl", SearchOption.AllDirectories)
                                   .Where(f => !f.Contains("backup", StringComparison.OrdinalIgnoreCase)));
        }

        // Remove duplicates
        files = files.Distinct().ToList();

        AppLogManager.Log($"Found {files.Count} .pkl files total");

        // Log the files we found for debugging
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(PathRepoPGET, file);
            AppLogManager.LogDebug($"Found: {relativePath}");
        }

        int ctr = 0;
        var expectedFiles = new[] { "encounter_go_home.pkl", "encounter_go_lgpe.pkl" };
        var foundExpected = new HashSet<string>();

        foreach (var file in files)
        {
            try
            {
                var filename = Path.GetFileName(file);
                var destFile = Path.Combine(dest, filename);
                File.Copy(file, destFile, true);
                AppLogManager.Log($"Copied {filename} to {dest}");
                ctr++;

                if (expectedFiles.Any(ef => filename.Contains(ef) || ef.Contains(filename)))
                {
                    foundExpected.Add(filename);
                }
            }
            catch (Exception ex)
            {
                AppLogManager.LogWarning($"Failed to copy {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        if (ctr == 0)
        {
            AppLogManager.LogError($"No .pkl files found in {searchDir} or its subdirectories");
            AppLogManager.LogError("PGET may have failed to generate the pickle files");

            // List what's in the directory for debugging
            var allFiles = Directory.GetFiles(searchDir, "*.*", SearchOption.TopDirectoryOnly).Take(10);
            AppLogManager.LogDebug($"Files in exe directory: {string.Join(", ", allFiles.Select(Path.GetFileName))}");
        }
        else
        {
            AppLogManager.Log($"Successfully copied {ctr} pickle files to {dest}");

            if (foundExpected.Count > 0)
            {
                AppLogManager.Log($"Found expected files: {string.Join(", ", foundExpected)}");
            }

            var missingExpected = expectedFiles.Where(ef => !foundExpected.Any(fe => fe.Contains(ef) || ef.Contains(fe))).ToList();
            if (missingExpected.Any())
            {
                AppLogManager.LogWarning($"Missing expected files: {string.Join(", ", missingExpected)}");
            }
        }
    }
}
