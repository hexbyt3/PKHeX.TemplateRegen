using System.Reflection;
using System.Text.Json;
using PKHeX.TemplateRegen;

Console.WriteLine("Hello, World!");

// Attach Console to NLog's logger - Info
// Only log the message
var config = new NLog.Config.LoggingConfiguration();
var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);
NLog.LogManager.Configuration = config;

if (RunUpdate(Assembly.GetEntryAssembly()!.Location)) 
    Console.WriteLine("Done!");

Console.ReadKey();
return;

bool RunUpdate(string localPath)
{
    var localDir = Path.GetDirectoryName(localPath) ?? string.Empty;

    const string jsonName = "settings.json";
    // Grab the settings from the executable's path
    // If the settings file does not exist, create it with default values
    // If the settings file exists, read it and use the values

    var settingsPath = Path.Combine(localDir, jsonName);
    if (!File.Exists(settingsPath))
    {
        LogUtil.Log($"{jsonName} not found. Creating default.");
        WriteDefaultSettings(settingsPath);
        return false;
    }
    var text = File.ReadAllText(settingsPath);
    if (JsonSerializer.Deserialize(text, ProgramSettingsContext.Default.ProgramSettings) is not { } settings)
    {
        LogUtil.Log($"{jsonName} is invalid. Overwriting with default.");
        WriteDefaultSettings(settingsPath);
        return false;
    }

    if (!Directory.Exists(settings.PathPKHeX))
    {
        LogUtil.Log("resource path not found");
        return false;
    }


    var mgdb = new MGDBPickler(settings.PathPKHeX, settings.PathRepoEvGal);
    mgdb.Update();

    var pget = new PGETPickler(settings.PathPKHeX, settings.PathRepoPGET);
    pget.Update();
    return true;
}

static void WriteDefaultSettings(string path)
{
    var result = new ProgramSettings();
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };
    var json = JsonSerializer.Serialize(result, options);
    File.WriteAllText(path, json);
}
