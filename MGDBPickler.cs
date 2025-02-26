namespace PKHeX.TemplateRegen;

public class MGDBPickler(string PKHeXLegality, string EventGalleryRepoPath)
{
    private const string LegalityOverrideCards = "PKHeX Legality";

    private static Dictionary<string, string> BadCardSwap = new()
    {
        {"1053 XYORAS - 데세르시티 Arceus (KOR).wc6",
         "1053 XYORAS - 데세르시티 Arceus (KOR) - Form Fix.wc6"},
        {"0146 SWSH - サトシ Dracovish.wc8",
         "0146 SWSH - サトシ Dracovish - Gender Fix.wc8"},
    };

    public void Update()
    {
        var repoPath = EventGalleryRepoPath;
        if (!RepoUpdater.UpdateRepo("EventsGallery", repoPath, "master"))
            return;

        var released = Path.Combine(repoPath, "Released");
        string _9 = Path.Combine(released, "Gen 9");
        string _8a = Path.Combine(released, "Gen 8");
        string _8b = Path.Combine(released, "Gen 8");
        string _8 = Path.Combine(released, "Gen 8");
        string _7b = Path.Combine(released, "Gen 7", "Switch", "Wondercards");
        string _7 = Path.Combine(released, "Gen 7", "3DS", "Wondercards");
        string _6 = Path.Combine(released, "Gen 6");
        string _5 = Path.Combine(released, "Gen 5");
        string _4 = Path.Combine(released, "Gen 4", "Wondercards");

        Bin(_4, "wc4");
        Bin(_5, "pgf");
        Bin(_6, "wc6", "wc6full");
        Bin(_7, "wc7", "wc7full");
        Bin(_7b, "wb7full");
        Bin(_8, "wc8");
        Bin(_8b, "wb8");
        Bin(_8a, "wa8");
        Bin(_9, "wc9");
    }

    private void Bin(string path, params string[] type)
    {
        var dest = Path.Combine(PKHeXLegality, "mgdb");
        foreach (var z in type)
            BinWrite(dest, path, z);
    }

    private void BinWrite(string outDir, string path, string ext)
    {
        if (!Directory.Exists(path))
            LogUtil.Log($"input path not found ({ext})");
        else
            BinFiles(path, ext, Path.Combine(outDir, $"{ext}.pkl"));
    }

    private void BinFiles(string directory, string ext, string outfile)
    {
        // create/clear file
        File.WriteAllBytes(outfile, []);
        using var stream = new FileStream(outfile, FileMode.Append);

        var files = Directory.EnumerateFiles(directory, $"*.{ext}", SearchOption.AllDirectories);
        int ctr = 0;
        foreach (var f in files)
        {
            var file = f;
            if (!f.EndsWith(ext)) // Double check
                continue;

            var fileName = Path.GetFileName(f);
            if (BadCardSwap.TryGetValue(fileName, out var redirect))
                file = Path.Combine(EventGalleryRepoPath, LegalityOverrideCards, redirect);

            var bytes = File.ReadAllBytes(file);
            stream.Write(bytes);
            ctr++;
        }
        LogUtil.Log($"{ext}: {ctr}");
    }
}
