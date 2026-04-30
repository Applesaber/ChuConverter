using System.Globalization;
using System.Text;
using ChuConverter.Models;

namespace ChuConverter;

public static class UgcSerializer
{
    public static string Serialize(UgcChart ugc)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"' Converted from C2S by ChuConverter");
        sb.AppendLine($"@VER\t{ugc.Version}");
        if (!string.IsNullOrEmpty(ugc.Title))
            sb.AppendLine($"@TITLE\t{ugc.Title}");
        if (!string.IsNullOrEmpty(ugc.Artist))
            sb.AppendLine($"@ARTIST\t{ugc.Artist}");
        if (!string.IsNullOrEmpty(ugc.Designer))
            sb.AppendLine($"@DESIGN\t{ugc.Designer}");
        sb.AppendLine($"@DIFF\t{DifficultyId(ugc.Difficulty)}");
        sb.AppendLine($"@LEVEL\t{ugc.Level}");
        sb.AppendLine($"@CONST\t{ugc.Constant:F5}");
        if (!string.IsNullOrEmpty(ugc.SongId))
            sb.AppendLine($"@SONGID\t{ugc.SongId.PadLeft(4, '0')}");

        sb.AppendLine($"@TICKS\t{ugc.TicksPerBeat}");

        foreach (var b in ugc.BeatEvents.OrderBy(b => b.Measure))
            sb.AppendLine($"@BEAT\t{b.Measure}\t{b.Numerator}\t{b.Denominator}");

        foreach (var b in ugc.BpmEvents.OrderBy(b => b.Measure).ThenBy(b => b.Offset))
            sb.AppendLine($"@BPM\t{b.Measure}'{b.Offset}\t{b.Bpm:F5}");

        if (ugc.SpeedEvents.Count > 0)
        {
            foreach (var s in ugc.SpeedEvents.OrderBy(s => s.Measure).ThenBy(s => s.Offset))
                sb.AppendLine($"@TIL\t{s.Measure}\t{s.Measure}'{s.Offset}\t{s.Multiplier:F5}");
            sb.AppendLine($"@MAINTIL\t{ugc.SpeedEvents[^1].Measure}");
        }
        else
        {
            sb.AppendLine("@TIL\t0\t0'0\t1.00000");
            sb.AppendLine("@MAINTIL\t0");
        }

        sb.AppendLine("@ENDHEAD");
        sb.AppendLine();

        WriteNotes(ugc, sb);

        return sb.ToString();
    }

    private static void WriteNotes(UgcChart ugc, StringBuilder sb)
    {
        var notes = ugc.Notes.OrderBy(n => n.Measure).ThenBy(n => n.Offset).ToList();

        for (int i = 0; i < notes.Count; i++)
        {
            var n = notes[i];
            sb.Append(FormatNoteLine(n));

            if (n.Type == NoteType.HLD && n.HoldDuration > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"#{n.HoldDuration}>s");
            }
            else if ((n.Type == NoteType.SLD || n.Type == NoteType.SXD) && n.SlideDuration > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"#{n.SlideDuration}>s{HexCell(n.EndCell)}{HexWidth(n.EndWidth)}");
            }
            else
            {
                sb.AppendLine();
            }
        }
    }

    private static string FormatNoteLine(UgcNote n)
    {
        string pos = $"#{n.Measure}'{n.Offset}:";
        string typeCode = UTypeCode(n);

        return pos + typeCode;
    }

    private static string UTypeCode(UgcNote n)
    {
        string cell = HexCell(n.Cell);
        string width = HexWidth(n.Width);

        return n.Type switch
        {
            NoteType.TAP => $"t{cell}{width}",
            NoteType.CHR => $"x{cell}{width}{AnimCode(n.Extra)}",
            NoteType.HLD => $"h{cell}{width}",
            NoteType.SLD or NoteType.SXD => $"s{cell}{width}",
            NoteType.SLC or NoteType.SXC => $"s{cell}{width}",
            NoteType.FLK => $"f{cell}{width}A",
            NoteType.MNE => $"d{cell}{width}",
            NoteType.AIR => $"a{cell}{width}UC{n.TargetNote}",
            NoteType.AUR => $"a{cell}{width}UR{n.TargetNote}",
            NoteType.AUL => $"a{cell}{width}UL{n.TargetNote}",
            NoteType.AHD => $"a{cell}{width}HD{n.TargetNote}_{n.AirHoldDuration}",
            NoteType.ADW => $"a{cell}{width}DC{n.TargetNote}",
            NoteType.ADR => $"a{cell}{width}DR{n.TargetNote}",
            NoteType.ADL => $"a{cell}{width}DL{n.TargetNote}",
            NoteType.ALD => $"a{cell}{width}LD{n.TargetNote}",
            NoteType.ASD => $"a{cell}{width}SD{n.TargetNote}",
            _ => $"t{cell}{width}"
        };
    }

    private static string HexCell(int c) => c switch
    {
        >= 0 and <= 9 => ((char)('0' + c)).ToString(),
        >= 10 => ((char)('A' + c - 10)).ToString(),
        _ => "0"
    };

    private static string HexWidth(int w) => w switch
    {
        >= 1 and <= 9 => ((char)('0' + w)).ToString(),
        >= 10 => ((char)('A' + w - 10)).ToString(),
        _ => "1"
    };

    private static string AnimCode(string extra) => extra switch
    {
        "UP" => "UP",
        "DW" => "DW",
        "CE" => "CE",
        _ => "UP"
    };

    private static int DifficultyId(string diff) => diff.ToUpperInvariant() switch
    {
        "BASIC" => 0,
        "ADVANCED" => 1,
        "EXPERT" => 2,
        "MASTER" => 3,
        "ULTIMA" => 4,
        _ => 0
    };
}
