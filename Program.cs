using System.Text.Json;
using PKHeX.TemplateRegen;

Console.WriteLine("Hello, World!");

var settings = GetSettings();
if (!Directory.Exists(settings.RepoPathPKHeX))
{
	Console.WriteLine("resource path not found");
	return;
}

var mgdb = new MGDBPickler(settings.RepoPathPKHeX, settings.RepoPathEvGal);
mgdb.Update();

Console.WriteLine("Done!");
Console.ReadKey();
return;

static ProgramSettings GetSettings()
{
    const string path = "settings.json";

    ProgramSettings result;
    if (!File.Exists(path))
    {
        result = new ProgramSettings();
        File.WriteAllText(path, JsonSerializer.Serialize(result, ProgramSettingsContext.Default.ProgramSettings));
    }
    else
    {
        var text = File.ReadAllText(path);
        result = JsonSerializer.Deserialize(text, ProgramSettingsContext.Default.ProgramSettings) ?? new();
    }
    return result;
}