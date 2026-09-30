using System.Buffers.Binary;
using System.Text.RegularExpressions;

namespace PKHeX.TemplateRegen.Core;

/// <summary>
/// Checks that the GO pickles PoGoEncTool produced use the slot layout the target PKHeX reads.
/// </summary>
/// <remarks>
/// PoGoEncTool changed its layout on 2026-09-23 (10-byte slots became 12-byte slots). PKHeX reads a
/// pickle of the wrong layout without complaint and silently mis-parses every slot, so a PKHeX that
/// has not been updated must never receive the new pickles, and vice versa.
/// </remarks>
public static partial class GoPickleLayout
{
    private const int AreaHeaderSize = 4; // species u16, form u8, import format u8

    [GeneratedRegex(@"entrySize\s*=\s*\(\s*(\d+)\s*\*\s*sizeof\((int|ushort)\)\s*\)\s*\+\s*(\d+)")]
    private static partial Regex EntrySizeRegex();

    /// <summary>
    /// Reads the slot size from the target PKHeX's GO area reader, or null if it can't be found.
    /// </summary>
    /// <param name="legalityPath">The PKHeX.Core Resources/legality folder the pickles are copied into.</param>
    /// <param name="areaFile">EncounterArea8g.cs for HOME pickles, EncounterArea7g.cs for LGPE.</param>
    public static int? GetExpectedSlotSize(string legalityPath, string areaFile)
    {
        var coreDir = Directory.GetParent(legalityPath)?.Parent?.FullName;
        if (coreDir is null)
            return null;

        var source = Path.Combine(coreDir, "Legality", "Encounters", "Templates", "GO", areaFile);
        if (!File.Exists(source))
            return null;

        var match = EntrySizeRegex().Match(File.ReadAllText(source));
        if (!match.Success)
            return null;

        int count = int.Parse(match.Groups[1].Value);
        int width = match.Groups[2].Value == "int" ? sizeof(int) : sizeof(ushort);
        return (count * width) + int.Parse(match.Groups[3].Value);
    }

    /// <summary>
    /// Returns null if every area in the pickle is a whole number of slots of <paramref name="slotSize"/>,
    /// otherwise a description of the first area that isn't.
    /// </summary>
    public static string? Validate(ReadOnlySpan<byte> pickle, int slotSize)
    {
        if (pickle.Length < 4)
            return "file is too short to be a pickle";

        int count = BinaryPrimitives.ReadUInt16LittleEndian(pickle[2..]);
        if (count == 0)
            return "pickle has no areas";

        for (int i = 0; i < count; i++)
        {
            int offset = 4 + (i * sizeof(int));
            if (offset + 8 > pickle.Length)
                return $"offset table is truncated at area {i}";

            int start = BinaryPrimitives.ReadInt32LittleEndian(pickle[offset..]);
            int end = BinaryPrimitives.ReadInt32LittleEndian(pickle[(offset + 4)..]);
            if (start < 0 || end > pickle.Length || end - start < AreaHeaderSize)
                return $"area {i} has an invalid range {start}..{end}";

            int slotBytes = end - start - AreaHeaderSize;
            if (slotBytes % slotSize != 0)
                return $"area {i} holds {slotBytes} bytes of slots, not a multiple of {slotSize}";
        }
        return null;
    }
}
