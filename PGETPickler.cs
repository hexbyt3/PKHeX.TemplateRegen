using System.Diagnostics;

namespace PKHeX.TemplateRegen;

public class PGETPickler(string PathPKHeXLegality, string PathRepoPGET)
{
    public void Update()
    {
        var exe = PathRepoPGET;
        if (!File.Exists(exe))
        {
            // find the first file with exe extension in the folder
            exe = Directory.EnumerateFiles(PathRepoPGET, "*.exe", SearchOption.AllDirectories).FirstOrDefault(z => z.Contains("WinForms"));
            if (exe is null)
            {
                LogUtil.Log("PGET executable not found");
                return;
            }
        }

        if (!RepoUpdater.UpdateRepo("pget", PathRepoPGET, "main"))
            return;

        // start the executable with --update passed as arg
        var startInfo = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = "--update",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(exe) ?? string.Empty,
            RedirectStandardError = true,
        };
        using var process = Process.Start(startInfo);
        if (process == null)
        {
            LogUtil.Log($"Failed to start {exe} executable");
            return;
        }
        process.WaitForExit();

        // Get all the created .pkl files then copy them to the destination folder
        var dest = Path.Combine(PathPKHeXLegality, "wild");
        var files = Directory.EnumerateFiles(Path.GetDirectoryName(exe) ?? string.Empty, "*.pkl", SearchOption.AllDirectories);
        int ctr = 0;
        foreach (var file in files)
        {
            var filename = Path.GetFileName(file);
            var destFile = Path.Combine(dest, filename);
            File.Copy(file, destFile, true);
            LogUtil.Log($"Copied {filename} to {dest}");
            ctr++;
        }
        LogUtil.Log($"Copied {ctr} files to {dest}");
    }
}
