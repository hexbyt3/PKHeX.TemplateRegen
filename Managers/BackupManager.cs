namespace PKHeX.TemplateRegen.Managers;

public static class BackupManager
{
    private static readonly string BackupDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
    private const int MaxBackups = 10;

    static BackupManager()
    {
        if (!Directory.Exists(BackupDirectory))
            Directory.CreateDirectory(BackupDirectory);
    }

    public static void CreateBackup(string pkhexPath)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupPath = Path.Combine(BackupDirectory, $"backup_{timestamp}");
            Directory.CreateDirectory(backupPath);

            var mgdbPath = Path.Combine(pkhexPath, "mgdb");
            if (Directory.Exists(mgdbPath))
            {
                var mgdbBackup = Path.Combine(backupPath, "mgdb");
                Directory.CreateDirectory(mgdbBackup);

                foreach (var file in Directory.GetFiles(mgdbPath, "*.pkl"))
                {
                    File.Copy(file, Path.Combine(mgdbBackup, Path.GetFileName(file)), true);
                }
            }

            var wildPath = Path.Combine(pkhexPath, "wild");
            if (Directory.Exists(wildPath))
            {
                var wildBackup = Path.Combine(backupPath, "wild");
                Directory.CreateDirectory(wildBackup);

                foreach (var file in Directory.GetFiles(wildPath, "*.pkl"))
                {
                    File.Copy(file, Path.Combine(wildBackup, Path.GetFileName(file)), true);
                }
            }

            AppLogManager.Log($"Backup created: {backupPath}");

            CleanOldBackups();
        }
        catch (Exception ex)
        {
            AppLogManager.LogError($"Error creating backup: {ex.Message}", ex);
        }
    }

    private static void CleanOldBackups()
    {
        try
        {
            var backups = Directory.GetDirectories(BackupDirectory)
                                  .OrderByDescending(d => Directory.GetCreationTime(d))
                                  .Skip(MaxBackups)
                                  .ToList();

            foreach (var backup in backups)
            {
                Directory.Delete(backup, true);
                AppLogManager.Log($"Old backup deleted: {Path.GetFileName(backup)}");
            }
        }
        catch (Exception ex)
        {
            AppLogManager.LogError($"Error cleaning old backups: {ex.Message}", ex);
        }
    }

    public static List<string> GetBackups()
    {
        try
        {
            return Directory.GetDirectories(BackupDirectory)
                          .OrderByDescending(d => Directory.GetCreationTime(d))
                          .Select(Path.GetFileName)
                          .Where(name => !string.IsNullOrEmpty(name))
                          .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    public static void RestoreBackup(string backupName, string pkhexPath)
    {
        try
        {
            var backupPath = Path.Combine(BackupDirectory, backupName);
            if (!Directory.Exists(backupPath))
            {
                AppLogManager.LogError($"Backup not found: {backupName}");
                return;
            }

            var mgdbBackup = Path.Combine(backupPath, "mgdb");
            if (Directory.Exists(mgdbBackup))
            {
                var mgdbPath = Path.Combine(pkhexPath, "mgdb");
                Directory.CreateDirectory(mgdbPath);

                foreach (var file in Directory.GetFiles(mgdbBackup, "*.pkl"))
                {
                    File.Copy(file, Path.Combine(mgdbPath, Path.GetFileName(file)), true);
                }
            }

            var wildBackup = Path.Combine(backupPath, "wild");
            if (Directory.Exists(wildBackup))
            {
                var wildPath = Path.Combine(pkhexPath, "wild");
                Directory.CreateDirectory(wildPath);

                foreach (var file in Directory.GetFiles(wildBackup, "*.pkl"))
                {
                    File.Copy(file, Path.Combine(wildPath, Path.GetFileName(file)), true);
                }
            }

            AppLogManager.Log($"Backup restored: {backupName}");
        }
        catch (Exception ex)
        {
            AppLogManager.LogError($"Error restoring backup: {ex.Message}", ex);
        }
    }
}
