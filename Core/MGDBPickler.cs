using System.Runtime.InteropServices;

namespace PKHeX.TemplateRegen.Core;

public class MGDBPickler(string PKHeXLegality, string EventGalleryRepoPath, bool AutoManage)
{
    private const string LegalityOverrideCards = "PKHeX Legality";

    // PGF.Size in PKHeX.Core. Kept local so the tool does not take a Core reference.
    private const int PgfSize = 0xCC;

    // WC5Full.Size in PKHeX.Core -- the full presentation card.
    private const int Wc5FullSize = 0x2D0;

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
        const string EventsGalleryRepoUrl = "https://github.com/projectpokemon/EventsGallery";

        AppLogManager.Log("Starting Events Gallery update...");

        if (AutoManage)
        {
            AppLogManager.Log("Auto-management enabled: Checking EventsGallery repository...");

            // Use Git CLI for EventsGallery to handle long paths on Windows
            var repoResult = RepoUpdater.CloneOrUpdateRepoViaCli(
                "EventsGallery",
                EventsGalleryRepoUrl,
                repoPath,
                "master"
            );

            if (!repoResult.Success)
            {
                AppLogManager.LogError("Failed to clone or update Events Gallery repository");
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
            }
            else
            {
                AppLogManager.Log($"Repository already up to date (commit {repoResult.CommitHash?[..7]})");
            }
        }
        else
        {
            AppLogManager.Log("Auto-management disabled: Using existing repository at specified path");
            if (!Directory.Exists(repoPath))
            {
                AppLogManager.LogError($"EventsGallery repository not found at: {repoPath}");
                AppLogManager.LogError("Please ensure the repository exists or enable auto-management");
                return;
            }
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
            ["Gen 9"] = [Path.Combine(released, "Gen 9"), "wc9", "wa9"]
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
            if (ext == "pgf")
                BinFilesPGF(path, ext, outfile);
            else
                BinFiles(path, ext, outfile);
        }
        catch (Exception ex)
        {
            AppLogManager.LogError($"Error processing {ext} files: {ex.Message}", ex);
        }
    }

    // Specialized pickler: PKHeX reads a receivability footer (one byte per gift) appended
    // after the concatenated cards. The constraints are scraped from the gift filename.
    private void BinFilesPGF(string directory, string ext, string outfile)
    {
        AppLogManager.Log($"Processing {ext} files (with receivability metadata)...");

        // Gen 5 ships 63 distributions only as .wc5full -- the full wonder card,
        // whose first PGF.Size bytes ARE the gift (see PKHeX WC5Full.cs, and
        // MysteryGift.cs which already reads the extension). Globbing "*.pgf"
        // alone dropped every one of them from pgf.pkl, so the bot rejected
        // Pokemon from those distributions as "Unable to match an encounter" --
        // Keldeo, Meloetta, Darkrai, Zoroark, Dialga/Palkia/Giratina among them.
        // Gen 6 and 7 already pickle their "full" variants; Gen 5 has no
        // wc5full.pkl, so the cards have to be folded into pgf.pkl instead.
        // The filename convention is identical, so GetReceivability5 needs no change.
        var files = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
                            .Where(f => f.EndsWith($".{ext}", StringComparison.OrdinalIgnoreCase)
                                     || f.EndsWith(".wc5full", StringComparison.OrdinalIgnoreCase))
                            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                            .ToList();

        if (files.Count == 0)
        {
            AppLogManager.LogWarning($"No {ext} files found in {directory}");
            File.WriteAllBytes(outfile, []);
            return;
        }

        // Build in memory so a mid-way failure cannot leave a truncated .pkl on disk.
        using var cards = new MemoryStream();
        List<byte> receivability = new(files.Count);
        var totalSize = 0L;

        foreach (var file in files)
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

            // A .wc5full is the 720-byte presentation card; the gift itself is the
            // leading PGF, exactly as WC5Full's constructor slices it. Every entry
            // written here must be PGF-sized, because the reader walks the pickle in
            // fixed-size strides and then expects one receivability byte per card in
            // the footer.
            //
            // Take that card's restrictions from its own metadata rather than from
            // the filename. WC5Full exposes RestrictVersion at Metadata[2] and
            // RestrictLanguage at Metadata[^5], where Metadata starts at PGF.Size --
            // real data, and the reason a .wc5full loaded straight from a folder
            // resolves when a filename-scraped one does not.
            if (bytes.Length >= Wc5FullSize)
            {
                var version = bytes[PgfSize + 2];
                var language = bytes[Wc5FullSize - 5];
                receivability.Add((byte)((language << 4) | (version & 0x0F)));
                bytes = bytes[..PgfSize];
            }
            else
            {
                // Plain .pgf carries no metadata; the constraints live in the name.
                // Scraped from the original filename, not the override.
                receivability.Add(GetReceivability5(Path.GetFileNameWithoutExtension(file)));
            }

            cards.Write(bytes);
            totalSize += bytes.Length;
        }

        using (var stream = new FileStream(outfile, FileMode.Create))
        {
            cards.Position = 0;
            cards.CopyTo(stream);
            stream.Write(CollectionsMarshal.AsSpan(receivability));
        }

        var sizeMB = totalSize / (1024.0 * 1024.0);
        AppLogManager.Log($"{ext}: Successfully processed {files.Count} files ({sizeMB:F2} MB) + {receivability.Count} receivability bytes");

        var fileInfo = new FileInfo(outfile);
        AppLogManager.LogDebug($"Created {Path.GetFileName(outfile)} - Size: {fileInfo.Length:N0} bytes");
    }

    private static byte GetReceivability5(string fileName)
    {
        // 0035 W - 잔타 Golurk (KOR).pgf
        // Second word is receivability: BWB2W2 mapped to bitflags
        // Last (*) is language.

        // Get Receivability: second word in the filename
        var parts = fileName.Split(' ');
        if (parts.Length < 2)
            throw new ArgumentException($"Invalid filename format: {fileName}");

        var resultVersion = GetVersionFromTag(parts[1]);
        var language = parts[^1];
        if (language.Length != 5) // bad tag
            language = parts[^2];
        var resultLanguage = GetLanguageFromTag(language);

        // Merge them together. Version first 4 bits, then language top 4 bits.
        return (byte)((resultLanguage << 4) | resultVersion);
    }

    private static byte GetVersionFromTag(ReadOnlySpan<char> tag)
    {
        byte result = 0;
        // peel off bitflags from the tag.

        // W=0, B=1, W2=2, B2=3
        if (tag.EndsWith("W2"))
        {
            result |= 1 << 2; // W2 flag
            tag = tag[..^2];
        }
        if (tag.EndsWith("B2"))
        {
            result |= 1 << 3; // B2 flag
            tag = tag[..^2];
        }
        if (tag.EndsWith("W"))
        {
            result |= 1 << 0; // W flag
            tag = tag[..^1];
        }
        if (tag.EndsWith("B"))
        {
            result |= 1 << 1; // B flag
            tag = tag[..^1];
        }
        return result;
    }

    private static byte GetLanguageFromTag(ReadOnlySpan<char> tag) => tag switch
    {
        "(JPN)" => 1, // Japanese
        "(ENG)" => 2, // English (US/UK/AU)
        "(FRE)" => 3, // French
        "(ITA)" => 4, // Italian
        "(GER)" => 5, // German
        "(SPA)" => 7, // Spanish
        "(KOR)" => 8, // Korean
        _ => throw new ArgumentException($"Unknown language tag: {tag}"),
    };

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
