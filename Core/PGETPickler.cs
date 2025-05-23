using System.Diagnostics;

namespace PKHeX.TemplateRegen;

public class PGETPickler(string PathPKHeXLegality, string PathRepoPGET)
{
    private const string DataJsonUrl = "https://raw.githubusercontent.com/projectpokemon/PoGoEncTool/refs/heads/main/Resources/data.json";
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromMinutes(2) };

    public async Task UpdateAsync()
    {
        AppLogManager.Log("Starting PoGo Enc Tool update...");

        var exe = PathRepoPGET;
        if (!File.Exists(exe) || !exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
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
                return;
            }
        }

        AppLogManager.Log($"Found PGET executable: {Path.GetFileName(exe)}");

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
}
