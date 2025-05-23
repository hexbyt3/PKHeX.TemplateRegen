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
            AppLogManager.LogError($"Error loading settings: {ex.Message}", ex);
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
            AppLogManager.LogError($"Error saving settings: {ex.Message}", ex);
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
            AppLogManager.LogError($"Error loading settings from file: {ex.Message}", ex);
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
