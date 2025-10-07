using System.Diagnostics;

namespace PKHeX.TemplateRegen;

public class PGETPickler(string PathPKHeXLegality, string PathRepoPGET, bool AutoManage = true)
{
    private const string DataJsonUrl = "https://raw.githubusercontent.com/projectpokemon/PoGoEncTool/refs/heads/main/Resources/data.json";
    private const string PoGoEncToolRepoUrl = "https://github.com/projectpokemon/PoGoEncTool";
    private const string RepoBranch = "main";
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromMinutes(2) };

    public async Task UpdateAsync()
    {
        AppLogManager.Log("Starting PoGo Enc Tool update...");

        Core.RepoUpdateResult? repoResult = null;
        bool needsRebuild = false;

        // Step 1: Auto-manage repository (clone/update) if enabled
        if (AutoManage)
        {
            AppLogManager.Log("Auto-management enabled: Checking PoGoEncTool repository...");
            AppLogManager.Log("This may take a moment on first run or when updates are available...");

            repoResult = Core.RepoUpdater.CloneOrUpdateRepo(
                "PoGoEncTool",
                PoGoEncToolRepoUrl,
                PathRepoPGET,
                RepoBranch
            );

            if (!repoResult.Success)
            {
                AppLogManager.LogError("Failed to clone or update PoGoEncTool repository");
                AppLogManager.LogError("Please check your internet connection and try again");
                if (!string.IsNullOrEmpty(repoResult.ErrorMessage))
                    AppLogManager.LogError($"Error details: {repoResult.ErrorMessage}");
                return;
            }

            if (repoResult.WasUpdated)
            {
                AppLogManager.Log($"Repository updated to commit {repoResult.CommitHash?[..7]}");
                if (!string.IsNullOrEmpty(repoResult.CommitMessage))
                    AppLogManager.Log($"Latest commit: {repoResult.CommitMessage}");
                needsRebuild = true;
            }
            else
            {
                AppLogManager.Log($"Repository already up to date (commit {repoResult.CommitHash?[..7]})");
            }
        }
        else
        {
            AppLogManager.Log("Auto-management disabled: Using existing PoGoEncTool repository");
        }

        // Step 2: Find or build the executable
        var exe = FindExecutable(PathRepoPGET);

        // Check if we need to rebuild
        if (AutoManage)
        {
            if (exe == null)
            {
                AppLogManager.Log("Executable not found, building PoGoEncTool...");
                needsRebuild = true;
            }
            else if (!needsRebuild)
            {
                // Check if exe is older than last commit
                var exeLastModified = File.GetLastWriteTimeUtc(exe);
                var commitTime = GetLastCommitTime(PathRepoPGET);

                if (commitTime.HasValue && exeLastModified < commitTime.Value)
                {
                    AppLogManager.Log("Executable is older than latest commit, rebuilding...");
                    needsRebuild = true;
                }
                else
                {
                    AppLogManager.Log("Using existing executable (up to date)");
                }
            }

            if (needsRebuild)
            {
                AppLogManager.Log("Building PoGoEncTool (this may take 10-30 seconds)...");
                var buildSuccess = Core.RepoUpdater.BuildProject("PoGoEncTool", PathRepoPGET);

                if (!buildSuccess)
                {
                    AppLogManager.LogError("Failed to build PoGoEncTool");
                    AppLogManager.LogError("Please ensure .NET SDK is installed (.NET 8.0 or later required)");
                    AppLogManager.LogError("Download from: https://dotnet.microsoft.com/download");
                    AppLogManager.LogError("");
                    AppLogManager.LogError("If you're using a custom build, you can disable auto-management in settings");
                    return;
                }

                // Re-find the executable after build
                exe = FindExecutable(PathRepoPGET);
            }
        }

        // Step 3: Verify executable exists
        if (exe == null)
        {
            exe = FindExecutable(PathRepoPGET);
        }

        if (exe == null)
        {
            AppLogManager.LogError("PGET executable not found");
            if (AutoManage)
            {
                AppLogManager.LogError("The build completed but the executable could not be located");
                AppLogManager.LogError("This is unexpected. Please report this issue.");
            }
            else
            {
                AppLogManager.LogError("Please build PoGoEncTool manually or enable auto-management in settings");
            }
            return;
        }

        AppLogManager.Log($"Using executable: {Path.GetFileName(exe)}");

        // Step 4: Download and update data.json
        AppLogManager.Log("Downloading latest data.json from GitHub...");
        string jsonContent;
        try
        {
            jsonContent = await HttpClient.GetStringAsync(DataJsonUrl);

            if (string.IsNullOrWhiteSpace(jsonContent) || jsonContent.Length < 10)
            {
                AppLogManager.LogError("Downloaded data.json appears to be invalid or empty");
                return;
            }

            AppLogManager.Log($"Successfully downloaded data.json ({jsonContent.Length:N0} characters)");
            AppLogManager.LogDebug($"data.json starts with: {jsonContent.Substring(0, Math.Min(50, jsonContent.Length))}...");
        }
        catch (HttpRequestException ex)
        {
            AppLogManager.LogError($"Failed to download data.json from GitHub: {ex.Message}");
            AppLogManager.LogError("Please check your internet connection and try again");
            return;
        }
        catch (Exception ex)
        {
            AppLogManager.LogError($"Unexpected error downloading data.json: {ex.Message}", ex);
            return;
        }

        var exeDir = Path.GetDirectoryName(exe) ?? string.Empty;
        var exeDataJson = Path.Combine(exeDir, "data.json");

        try
        {
            AppLogManager.Log("Saving data.json to exe directory...");
            await File.WriteAllTextAsync(exeDataJson, jsonContent);
            AppLogManager.Log($"Successfully saved data.json to: {exeDataJson}");

            var savedInfo = new FileInfo(exeDataJson);
            AppLogManager.LogDebug($"Saved file size: {savedInfo.Length:N0} bytes");
        }
        catch (Exception ex)
        {
            AppLogManager.LogError($"Failed to save data.json to exe directory: {ex.Message}", ex);
            return;
        }

        var repoDataJsonPath = Path.Combine(PathRepoPGET, "Resources", "data.json");
        try
        {
            var repoResourcesDir = Path.GetDirectoryName(repoDataJsonPath);
            if (repoResourcesDir != null && !Directory.Exists(repoResourcesDir))
            {
                Directory.CreateDirectory(repoResourcesDir);
                AppLogManager.LogDebug($"Created Resources directory: {repoResourcesDir}");
            }

            await File.WriteAllTextAsync(repoDataJsonPath, jsonContent);
            AppLogManager.Log($"Also updated repository's data.json at: {repoDataJsonPath}");
        }
        catch (Exception ex)
        {
            AppLogManager.LogWarning($"Failed to update repository's data.json: {ex.Message}");
        }

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

        process.WaitForExit();

        var exitCode = process.ExitCode;
        if (exitCode != 0)
        {
            AppLogManager.LogWarning($"PGET exited with code {exitCode}, but continuing to check for generated files...");
        }

        var dest = Path.Combine(PathPKHeXLegality, "wild");

        if (!Directory.Exists(dest))
        {
            Directory.CreateDirectory(dest);
            AppLogManager.Log($"Created wild directory: {dest}");
        }

        var searchDir = Path.GetDirectoryName(exe) ?? string.Empty;
        AppLogManager.Log($"Searching for .pkl files in: {searchDir}");

        var additionalSearchDirs = new[] { PathRepoPGET, Path.Combine(PathRepoPGET, "Resources") };

        var files = new List<string>();

        if (Directory.Exists(searchDir))
        {
            files.AddRange(Directory.EnumerateFiles(searchDir, "*.pkl", SearchOption.AllDirectories)
                                   .Where(f => !f.Contains("backup", StringComparison.OrdinalIgnoreCase)));
        }

        foreach (var dir in additionalSearchDirs.Where(d => Directory.Exists(d) && d != searchDir))
        {
            files.AddRange(Directory.EnumerateFiles(dir, "*.pkl", SearchOption.AllDirectories)
                                   .Where(f => !f.Contains("backup", StringComparison.OrdinalIgnoreCase)));
        }

        files = files.Distinct().ToList();

        AppLogManager.Log($"Found {files.Count} .pkl files total");

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

    public void Update()
    {
        UpdateAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Finds the PoGoEncTool executable in the repository, prioritizing Release builds.
    /// </summary>
    private static string? FindExecutable(string repoPath)
    {
        // Priority search paths - prefer Release over Debug, newer .NET versions first
        var searchPaths = new[]
        {
            Path.Combine(repoPath, "PoGoEncTool.WinForms", "bin", "Release", "net9.0-windows"),
            Path.Combine(repoPath, "PoGoEncTool.WinForms", "bin", "Release", "net8.0-windows"),
            Path.Combine(repoPath, "PoGoEncTool.WinForms", "bin", "Debug", "net9.0-windows"),
            Path.Combine(repoPath, "PoGoEncTool.WinForms", "bin", "Debug", "net8.0-windows"),
            Path.Combine(repoPath, "bin", "Release"),
            Path.Combine(repoPath, "bin", "Debug"),
            repoPath
        };

        // Search in priority order
        foreach (var searchPath in searchPaths.Where(Directory.Exists))
        {
            var exe = Directory.EnumerateFiles(searchPath, "*.exe", SearchOption.TopDirectoryOnly)
                              .FirstOrDefault(z => z.Contains("WinForms", StringComparison.OrdinalIgnoreCase) ||
                                                 z.Contains("PoGoEncTool", StringComparison.OrdinalIgnoreCase));
            if (exe != null)
            {
                AppLogManager.LogDebug($"Found executable in: {searchPath}");
                return exe;
            }
        }

        // Fallback: deep search in repo directory
        AppLogManager.LogDebug("Searching all subdirectories for executable...");
        return Directory.EnumerateFiles(repoPath, "*.exe", SearchOption.AllDirectories)
                       .Where(f => !f.Contains("backup", StringComparison.OrdinalIgnoreCase))
                       .FirstOrDefault(z => z.Contains("WinForms", StringComparison.OrdinalIgnoreCase) ||
                                          z.Contains("PoGoEncTool", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the timestamp of the last commit in the repository.
    /// </summary>
    private static DateTime? GetLastCommitTime(string repoPath)
    {
        try
        {
            if (!LibGit2Sharp.Repository.IsValid(repoPath))
                return null;

            using var repo = new LibGit2Sharp.Repository(repoPath);
            var lastCommit = repo.Head.Tip;
            return lastCommit?.Author.When.UtcDateTime;
        }
        catch (Exception ex)
        {
            AppLogManager.LogDebug($"Could not get last commit time: {ex.Message}");
            return null;
        }
    }
}
