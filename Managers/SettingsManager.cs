using System.Text.Json;
using System.Text.Json.Serialization;

namespace PKHeX.TemplateRegen;

public static class SettingsManager
{
    private const string SettingsFileName = "settings.json";
    private static readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static ProgramSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<ProgramSettings>(json, JsonOptions) ?? new ProgramSettings();
            }
        }
        catch (Exception ex)
        {
            AppLogManager.Log($"Error loading settings: {ex.Message}");
        }

        var defaultSettings = new ProgramSettings();
        SaveSettings(defaultSettings);
        return defaultSettings;
    }

    public static void SaveSettings(ProgramSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
            AppLogManager.Log("Settings saved successfully");
        }
        catch (Exception ex)
        {
            AppLogManager.Log($"Error saving settings: {ex.Message}");
        }
    }

    public static ProgramSettings? LoadSettingsFromFile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<ProgramSettings>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            AppLogManager.Log($"Error loading settings from file: {ex.Message}");
            return null;
        }
    }

    public static void SaveSettingsToFile(ProgramSettings settings, string filePath)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(filePath, json);
    }
}

public static class ProfileManager
{
    private static readonly string ProfilesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles");

    static ProfileManager()
    {
        if (!Directory.Exists(ProfilesDirectory))
            Directory.CreateDirectory(ProfilesDirectory);
    }

    public static List<string> GetProfileNames()
    {
        try
        {
            return Directory.GetFiles(ProfilesDirectory, "*.json")
                           .Select(Path.GetFileNameWithoutExtension)
                           .Where(name => !string.IsNullOrEmpty(name))
                           .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    public static void SaveProfile(string name, ProgramSettings settings)
    {
        var filePath = Path.Combine(ProfilesDirectory, $"{name}.json");
        SettingsManager.SaveSettingsToFile(settings, filePath);
        AppLogManager.Log($"Profile '{name}' saved");
    }

    public static ProgramSettings? LoadProfile(string name)
    {
        var filePath = Path.Combine(ProfilesDirectory, $"{name}.json");
        if (File.Exists(filePath))
        {
            return SettingsManager.LoadSettingsFromFile(filePath);
        }
        return null;
    }

    public static void DeleteProfile(string name)
    {
        var filePath = Path.Combine(ProfilesDirectory, $"{name}.json");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            AppLogManager.Log($"Profile '{name}' deleted");
        }
    }
}

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

            // Backup mgdb files
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

            // Backup wild files
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

            // Clean old backups
            CleanOldBackups();
        }
        catch (Exception ex)
        {
            AppLogManager.Log($"Error creating backup: {ex.Message}");
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
            AppLogManager.Log($"Error cleaning old backups: {ex.Message}");
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
                AppLogManager.Log($"Backup not found: {backupName}");
                return;
            }

            // Restore mgdb files
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

            // Restore wild files
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
            AppLogManager.Log($"Error restoring backup: {ex.Message}");
        }
    }
}
