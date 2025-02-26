namespace PKHeX.TemplateRegen;

using System.Text.Json.Serialization;

[JsonSerializable(typeof(ProgramSettings))]
public sealed partial class ProgramSettingsContext : JsonSerializerContext;

public class ProgramSettings
{
    public string RepoFolder { get; set; } = @"C:\Users\kapho\source\repos";
    public string RepoPKHeXLegality { get; set; } = @"PKHeX\PKHeX.Core\Resources\legality"; // where the legality files are stored

    public string RepoNameEvGal { get; set; } = "EventsGallery"; // relative if using RepoFolder
    public string RepoNamePGET { get; set; } = "PoGoEncTool"; // relative if using RepoFolder


    // Logic to get full paths
    public string PathPKHeX => GetRepoPath(RepoPKHeXLegality);
    public string PathRepoEvGal => GetRepoPath(RepoNameEvGal);
    public string PathRepoPGET => GetRepoPath(RepoNamePGET);

    private string GetRepoPath(string repoName)
    {
        // if path is absolute, return it
        if (Path.IsPathRooted(repoName))
            return repoName;
        // if path is relative, combine it with the repo folder
        return Path.Combine(RepoFolder, repoName);
    }
}
