using System.Text.RegularExpressions;

namespace PKHeX.TemplateRegen.Core;

/// <summary>
/// Checks that PoGoEncTool and the target PKHeX number the GO enums the same way.
/// </summary>
/// <remarks>
/// The pickles store PogoType and PogoBallRestriction as raw bytes. On 2026-11-16 PoGoEncTool
/// renumbered PogoBallRestriction (two NoSafari values went in the middle) without changing the
/// slot size, so a PKHeX on the old numbering would read every ball rule shifted and the layout
/// check would still pass.
/// </remarks>
public static partial class GoEnumCheck
{
    private static readonly string[] Enums = ["PogoType", "PogoBallRestriction"];

    [GeneratedRegex(@"^\s*([A-Za-z_]\w*)\s*(?:=\s*(\d+))?\s*,?\s*(?://.*)?$")]
    private static partial Regex MemberRegex();

    /// <summary>
    /// Returns null if every enum value PoGoEncTool writes means the same thing in PKHeX, otherwise what differs.
    /// </summary>
    public static string? Compare(string repoPGET, string legalityPath)
    {
        var coreDir = Directory.GetParent(legalityPath)?.Parent?.FullName;
        if (coreDir is null)
            return null;

        var pkhexSource = ReadAll(Path.Combine(coreDir, "Legality", "Encounters", "Templates", "GO"));
        var pgetSource = ReadAll(Path.Combine(repoPGET, "PoGoEncTool.Core", "Objects"));

        foreach (var name in Enums)
        {
            var theirs = Parse(pgetSource, name);
            var ours = Parse(pkhexSource, name);
            if (theirs is null || ours is null)
                continue;

            var byValue = ours.ToDictionary(z => z.Value, z => z.Key);
            foreach (var (member, value) in theirs)
            {
                if (!byValue.TryGetValue(value, out var mine))
                    return $"{name}.{member} = {value} has no value {value} in PKHeX";
                if (mine != member)
                    return $"{name} value {value} is {member} in PoGoEncTool but {mine} in PKHeX";
            }
        }
        return null;
    }

    private static string ReadAll(string dir) => !Directory.Exists(dir)
        ? string.Empty
        : string.Join('\n', Directory.EnumerateFiles(dir, "*.cs").Select(File.ReadAllText));

    /// <summary>
    /// Reads a plain enum's members and values from C# source; null if it isn't there.
    /// </summary>
    public static Dictionary<string, int>? Parse(string source, string name)
    {
        var start = Regex.Match(source, $@"\benum\s+{name}\b[^{{]*\{{");
        if (!start.Success)
            return null;

        var end = source.IndexOf('}', start.Index + start.Length);
        if (end < 0)
            return null;

        var result = new Dictionary<string, int>();
        int next = 0;
        foreach (var raw in source[(start.Index + start.Length)..end].Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//") || line.StartsWith('[') || line.StartsWith("/*") || line.StartsWith('*'))
                continue;

            var m = MemberRegex().Match(line);
            if (!m.Success)
                continue;

            var value = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : next;
            result[m.Groups[1].Value] = value;
            next = value + 1;
        }
        return result;
    }
}
