namespace PKHeX.TemplateRegen.Core;

public class MGDBPickler(string PKHeXLegality, string EventGalleryRepoPath)
{
    private const string LegalityOverrideCards = "PKHeX Legality";

    private static readonly Dictionary<string, string> BadCardSwap = new()
    {
        {"1053 XYORAS - 데세르시티 Arceus (KOR).wc6",
         "1053 XYORAS - 데세르시티 Arceus (KOR) - Form Fix.wc6"},
        {"0146 SWSH - サトシ Dracovish.wc8",
         "0146 SWSH - サトシ Dracovish - Gender Fix.wc8"},
    };

    public void Update()
    {
        var repoPath = EventGalleryRepoPath;

        AppLogManager.Log("Starting Events Gallery update...");

        if (!RepoUpdater.UpdateRepo("EventsGallery", repoPath, "master"))
        {
            AppLogManager.LogError("Failed to update Events Gallery repository");
            return;
        }

        var released = Path.Combine(repoPath, "Released");
        if (!Directory.Exists(released))
        {
            AppLogManager.LogError($"Released folder not found at: {released}");
            return;
        }

        // Define generation paths
        var generations = new Dictionary<string, string[]>
        {
            ["Gen 4"] = [Path.Combine(released, "Gen 4", "Wondercards"), "wc4"],
            ["Gen 5"] = [Path.Combine(released, "Gen 5"), "pgf"],
            ["Gen 6"] = [Path.Combine(released, "Gen 6"), "wc6", "wc6full"],
            ["Gen 7 (3DS)"] = [Path.Combine(released, "Gen 7", "3DS", "Wondercards"), "wc7", "wc7full"],
            ["Gen 7 (Switch)"] = [Path.Combine(released, "Gen 7", "Switch", "Wondercards"), "wb7full"],
            ["Gen 8"] = [Path.Combine(released, "Gen 8"), "wc8", "wb8", "wa8"],
            ["Gen 9"] = [Path.Combine(released, "Gen 9"), "wc9"]
        };

        var totalGens = generations.Count;
        var currentGen = 0;

        foreach (var (genName, pathAndTypes) in generations)
        {
            currentGen++;
            var progress = currentGen * 100 / totalGens;

            AppLogManager.Log($"Processing {genName} ({currentGen}/{totalGens})...");

            var path = pathAndTypes[0];
            var types = pathAndTypes.Skip(1).ToArray();

            Bin(path, types);
        }

        AppLogManager.Log("Events Gallery update completed successfully!");
    }

    private void Bin(string path, params string[] types)
    {
        var dest = Path.Combine(PKHeXLegality, "mgdb");

        if (!Directory.Exists(dest))
        {
            Directory.CreateDirectory(dest);
            AppLogManager.Log($"Created mgdb directory: {dest}");
        }

        foreach (var type in types)
        {
            BinWrite(dest, path, type);
        }
    }

    private void BinWrite(string outDir, string path, string ext)
    {
        if (!Directory.Exists(path))
        {
            AppLogManager.LogWarning($"Input path not found for {ext}: {path}");
            return;
        }

        var outfile = Path.Combine(outDir, $"{ext}.pkl");

        try
        {
            BinFiles(path, ext, outfile);
        }
        catch (Exception ex)
        {
            AppLogManager.LogError($"Error processing {ext} files: {ex.Message}", ex);
        }
    }

    private void BinFiles(string directory, string ext, string outfile)
    {
        AppLogManager.Log($"Processing {ext} files...");

        // Create/clear file
        File.WriteAllBytes(outfile, []);

        using var stream = new FileStream(outfile, FileMode.Append);

        var searchPattern = $"*.{ext}";
        var files = Directory.EnumerateFiles(directory, searchPattern, SearchOption.AllDirectories)
                            .Where(f => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                            .ToList();

        if (files.Count == 0)
        {
            AppLogManager.LogWarning($"No {ext} files found in {directory}");
            return;
        }

        var processed = 0;
        var skipped = 0;
        var totalSize = 0L;

        foreach (var file in files)
        {
            try
            {
                var targetFile = file;
                var fileName = Path.GetFileName(file);

                // Check for bad card replacements
                if (BadCardSwap.TryGetValue(fileName, out var redirect))
                {
                    var overridePath = Path.Combine(EventGalleryRepoPath, LegalityOverrideCards, redirect);
                    if (File.Exists(overridePath))
                    {
                        targetFile = overridePath;
                        AppLogManager.LogDebug($"Using override for {fileName}");
                    }
                    else
                    {
                        AppLogManager.LogWarning($"Override file not found: {redirect}");
                    }
                }

                var bytes = File.ReadAllBytes(targetFile);
                stream.Write(bytes);

                processed++;
                totalSize += bytes.Length;

                // Log progress every 100 files
                if (processed % 100 == 0)
                    AppLogManager.LogDebug($"{ext}: Processed {processed}/{files.Count} files");
            }
            catch (Exception ex)
            {
                AppLogManager.LogWarning($"Failed to process {Path.GetFileName(file)}: {ex.Message}");
                skipped++;
            }
        }

        stream.Flush();

        var sizeMB = totalSize / (1024.0 * 1024.0);
        AppLogManager.Log($"{ext}: Successfully processed {processed} files ({sizeMB:F2} MB), skipped {skipped}");

        // Verify the output file
        if (File.Exists(outfile))
        {
            var fileInfo = new FileInfo(outfile);
            AppLogManager.LogDebug($"Created {Path.GetFileName(outfile)} - Size: {fileInfo.Length:N0} bytes");
        }
    }
}
