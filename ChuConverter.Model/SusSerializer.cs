using System.Globalization;
using System.Text;
using ChuConverter.Models;

namespace ChuConverter;

public static class SusSerializer
{
    public static string Serialize(SusChart chart)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(chart.Title))
            sb.AppendLine($"#TITLE \"{chart.Title}\"");
        if (!string.IsNullOrEmpty(chart.Artist))
            sb.AppendLine($"#ARTIST \"{chart.Artist}\"");
        if (!string.IsNullOrEmpty(chart.Designer))
            sb.AppendLine($"#DESIGNER \"{chart.Designer}\"");

        sb.AppendLine($"#BPM_DEF {chart.Bpm:F2}");
        sb.AppendLine($"#REQUEST \"{chart.TicksPerBeat}\"");
        sb.AppendLine();

        foreach (var n in chart.Notes.OrderBy(n => n.Measure).ThenBy(n => n.Tick))
        {
            sb.AppendLine($"#{n.Measure:X2}{n.Tick:X2}:{FormatNoteData(n)}");
        }

        return sb.ToString();
    }

    private static string FormatNoteData(SusNote n)
    {
        string lw = $"{n.Lane:X2}{n.Width:X2}";

        return n.Type switch
        {
            SusNoteType.TAP => $"01{lw}",
            SusNoteType.CHR => $"02{lw}",
            SusNoteType.FLK => $"03{lw}",
            SusNoteType.HLD => $"05{lw}{n.Duration:X4}",
            SusNoteType.SLD => $"06{lw}{n.Duration:X4}{n.EndLane:X2}{n.EndWidth:X2}",
            SusNoteType.AIR => $"07{lw}{n.Target}",
            SusNoteType.AHD => $"08{lw}{n.Duration:X4}",
            SusNoteType.ADW => $"09{lw}{n.Target}",
            SusNoteType.MNE => $"10{lw}",
            _ => $"01{lw}"
        };
    }
}
