using System.Globalization;
using ChuConverter.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChuConverter;

public class C2sParser
{
    private readonly ILogger<C2sParser> _logger;

    public C2sParser(ILogger<C2sParser>? logger = null)
        => _logger = logger ?? NullLogger<C2sParser>.Instance;

    public static C2sChart Parse(string c2sText)
        => new C2sParser().ParseChart(c2sText);

    public C2sChart ParseChart(string c2sText)
    {
        _logger.LogInformation("开始解析 C2S, 文本长度: {Length} 字符", c2sText.Length);
        var chart = new C2sChart();
        var lines = SplitLines(c2sText);

        bool inBody = false;
        var noteLines = new List<string[]>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;

            var parts = line.Split('\t');

            if (inBody)
            {
                if (IsStatLine(parts[0]))
                    continue;
                noteLines.Add(parts);
                continue;
            }

            if (!IsHeaderOrTiming(parts[0]))
            {
                inBody = true;
                noteLines.Add(parts);
                continue;
            }

            switch (parts[0])
            {
                case "VERSION": chart.Version = Str(parts, 1); break;
                case "MUSIC": chart.MusicId = Int(parts, 1); break;
                case "DIFFICULT": chart.DifficultId = Int(parts, 1); break;
                case "RESOLUTION": chart.Resolution = Math.Max(1, Int(parts, 1, 384)); break;
                case "CREATOR": chart.Creator = Str(parts, 1); break;
                case "BPM_DEF": chart.BpmDef = Dbl(parts, 1, 120.0); break;
                case "BPM":
                    chart.BpmEvents.Add(new BpmEvent
                    {
                        Measure = Int(parts, 1),
                        Offset = Int(parts, 2),
                        Bpm = Dbl(parts, 3, 120.0)
                    });
                    break;
                case "MET":
                    chart.MetEvents.Add(new MetEvent
                    {
                        Measure = Int(parts, 1),
                        Offset = Int(parts, 2),
                        Denominator = Int(parts, 3, 4),
                        Numerator = Int(parts, 4, 4)
                    });
                    break;
                case "SFL":
                    chart.SflEvents.Add(new SflEvent
                    {
                        Measure = Int(parts, 1),
                        Offset = Int(parts, 2),
                        Duration = Int(parts, 3),
                        Multiplier = Dbl(parts, 4, 1.0)
                    });
                    break;
                case "CLK_DEF":
                case "PROGJUDGE_BPM":
                case "PROGJUDGE_AER":
                case "TUTORIAL":
                case "MET_DEF":
                case "LEVEL":
                case "SEQUENCEID":
                    break;
            }
        }

        foreach (var parts in noteLines)
        {
            var note = ParseNoteLine(parts);
            if (note != null)
                chart.Notes.Add(note);
        }

        _logger.LogInformation("C2S 解析完成: {NoteCount} 个音符, {BpmCount} 个 BPM, {SflCount} 个 SFL",
            chart.Notes.Count, chart.BpmEvents.Count, chart.SflEvents.Count);

        return chart;
    }

    private static ChartNote? ParseNoteLine(string[] p)
    {
        if (p.Length == 0) return null;

        var type = ParseNoteType(p[0]);

        return type switch
        {
            NoteType.TAP or NoteType.MNE => new ChartNote
            {
                Type = type,
                Measure = Int(p, 1),
                Offset = Int(p, 2),
                Cell = Int(p, 3),
                Width = Math.Max(1, Int(p, 4, 1))
            },
            NoteType.CHR => new ChartNote
            {
                Type = NoteType.CHR,
                Measure = Int(p, 1),
                Offset = Int(p, 2),
                Cell = Int(p, 3),
                Width = Math.Max(1, Int(p, 4, 1)),
                Extra = Str(p, 5)
            },
            NoteType.HLD or NoteType.HXD => new ChartNote
            {
                Type = type,
                Measure = Int(p, 1),
                Offset = Int(p, 2),
                Cell = Int(p, 3),
                Width = Math.Max(1, Int(p, 4, 1)),
                HoldDuration = Int(p, 5)
            },
            NoteType.SLD or NoteType.SLC or NoteType.SXD or NoteType.SXC => new ChartNote
            {
                Type = type,
                Measure = Int(p, 1),
                Offset = Int(p, 2),
                Cell = Int(p, 3),
                Width = Math.Max(1, Int(p, 4, 1)),
                SlideDuration = Int(p, 5),
                EndCell = Int(p, 6),
                EndWidth = Math.Max(1, Int(p, 7, 1))
            },
            NoteType.FLK => new ChartNote
            {
                Type = NoteType.FLK,
                Measure = Int(p, 1),
                Offset = Int(p, 2),
                Cell = Int(p, 3),
                Width = Math.Max(1, Int(p, 4, 1)),
                Extra = Str(p, 5)
            },
            NoteType.AIR or NoteType.AUR or NoteType.AUL
             or NoteType.ADW or NoteType.ADR or NoteType.ADL => new ChartNote
            {
                Type = type,
                Measure = Int(p, 1),
                Offset = Int(p, 2),
                Cell = Int(p, 3),
                Width = Math.Max(1, Int(p, 4, 1)),
                TargetNote = Str(p, 5)
            },
            NoteType.AHD => new ChartNote
            {
                Type = NoteType.AHD,
                Measure = Int(p, 1),
                Offset = Int(p, 2),
                Cell = Int(p, 3),
                Width = Math.Max(1, Int(p, 4, 1)),
                TargetNote = Str(p, 5),
                AirHoldDuration = Int(p, 6)
            },
            NoteType.ALD or NoteType.ASD => new ChartNote
            {
                Type = type,
                Measure = Int(p, 1),
                Offset = Int(p, 2),
                StartHeight = Int(p, 3),
                SlideDuration = Int(p, 4),
                EndCell = Int(p, 5),
                EndWidth = Math.Max(1, Int(p, 6, 1)),
                TargetHeight = Int(p, 7),
                NoteColor = Str(p, 8)
            },
            _ => null
        };
    }

    private static NoteType ParseNoteType(string s) => s.ToUpperInvariant() switch
    {
        "TAP" => NoteType.TAP,
        "CHR" => NoteType.CHR,
        "HLD" => NoteType.HLD,
        "HXD" => NoteType.HXD,
        "SLD" => NoteType.SLD,
        "SLC" => NoteType.SLC,
        "SXD" => NoteType.SXD,
        "SXC" => NoteType.SXC,
        "FLK" => NoteType.FLK,
        "AIR" => NoteType.AIR,
        "AUR" => NoteType.AUR,
        "AUL" => NoteType.AUL,
        "AHD" => NoteType.AHD,
        "ADW" => NoteType.ADW,
        "ADR" => NoteType.ADR,
        "ADL" => NoteType.ADL,
        "ALD" => NoteType.ALD,
        "ASD" => NoteType.ASD,
        "MNE" => NoteType.MNE,
        _ => NoteType.TAP
    };

    private static bool IsHeaderOrTiming(string s) => s switch
    {
        "VERSION" or "MUSIC" or "SEQUENCEID" or "DIFFICULT" or "LEVEL"
          or "CREATOR" or "BPM_DEF" or "MET_DEF" or "RESOLUTION" or "CLK_DEF"
          or "PROGJUDGE_BPM" or "PROGJUDGE_AER" or "TUTORIAL"
          or "BPM" or "MET" or "SFL" => true,
        _ => false
    };

    private static bool IsStatLine(string s) => s.StartsWith("T_");

    private static string[] SplitLines(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');

    private static int Int(string[] p, int i, int def = 0)
        => i < p.Length && int.TryParse(p[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : def;

    private static double Dbl(string[] p, int i, double def = 0)
        => i < p.Length && double.TryParse(p[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;

    private static string Str(string[] p, int i) => i < p.Length ? p[i] : "";
}
