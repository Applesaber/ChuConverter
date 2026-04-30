using System.Globalization;
using ChuConverter.Models;

namespace ChuConverter;

public class SusParser
{
    public static SusChart Parse(string susText)
    {
        var chart = new SusChart();
        var lines = susText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        var pendingHld = new Dictionary<int, SusNote>();
        var buildLines = new List<(int measure, int tick, SusNote note)>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || !line.StartsWith('#')) continue;

            if (line.StartsWith("#TITLE "))
                chart.Title = StripQuotes(line[7..]);
            else if (line.StartsWith("#ARTIST "))
                chart.Artist = StripQuotes(line[8..]);
            else if (line.StartsWith("#DESIGNER "))
                chart.Designer = StripQuotes(line[10..]);
            else if (line.StartsWith("#BPM_DEF "))
                chart.Bpm = ParseDouble(line[9..], 120.0);
            else if (line.StartsWith("#BPM ") || line.StartsWith("#BPM\t"))
                chart.Bpm = ParseDouble(line[5..], chart.Bpm);
            else if (line.StartsWith("#REQUEST "))
                chart.TicksPerBeat = int.TryParse(StripQuotes(line[9..]), out var t) ? t : 480;

            if (line.Length < 6 || line[1] > '9' || line[1] < '0') continue;

            var note = ParseSusNote(line, chart.TicksPerBeat);
            if (note != null)
                buildLines.Add(note.Value);
        }

        foreach (var (m, t, note) in buildLines)
        {
            if (note.Type == SusNoteType.HLD && note.Duration == 0)
            {
                var key = m * chart.TicksPerBeat * 4 + t;
                pendingHld[key] = note;
            }
            else
            {
                chart.Notes.Add(note);
            }
        }

        chart.Notes.Sort((a, b) =>
        {
            int c = a.Measure.CompareTo(b.Measure);
            return c != 0 ? c : a.Tick.CompareTo(b.Tick);
        });

        return chart;
    }

    private static (int measure, int tick, SusNote note)? ParseSusNote(string line, int tpb)
    {
        if (line.Length < 10) return null;

        int colon = line.IndexOf(':');
        if (colon < 5) return null;

        var posPart = line[1..colon];
        var dataPart = line[(colon + 1)..];

        if (!int.TryParse(posPart[..2], NumberStyles.HexNumber, null, out int measure)) return null;
        if (!int.TryParse(posPart[2..], NumberStyles.HexNumber, null, out int tick)) return null;

        var note = ParseSusNoteData(dataPart);
        if (note == null) return null;

        note.Measure = measure;
        note.Tick = tick;
        return (measure, tick, note);
    }

    private static SusNote? ParseSusNoteData(string data)
    {
        if (data.Length < 2) return null;

        string typeStr = data[..2];

        return typeStr switch
        {
            "1" or "01" => TapNote(data),
            "2" or "02" => ChrNote(data),
            "3" or "03" => FlkNote(data),
            "5" or "05" => HoldNote(data, SusNoteType.HLD),
            "6" or "06" => SlideNote(data),
            "7" or "07" => AirNote(data, SusNoteType.AIR),
            "8" or "08" => HoldNote(data, SusNoteType.AHD),
            "9" or "09" => AirNote(data, SusNoteType.ADW),
            "10" => MineNote(data),
            _ => ParseSimpleNote(data)
        };
    }

    private static SusNote? ParseSimpleNote(string data)
    {
        if (data.Length < 4) return null;
        return new SusNote
        {
            Type = SusNoteType.TAP,
            Lane = HexVal(data[..2]),
            Width = Math.Max(1, HexVal(data[2..4]))
        };
    }

    private static SusNote TapNote(string data) => ParseLaneWidth(data, SusNoteType.TAP);
    private static SusNote ChrNote(string data) => ParseLaneWidth(data, SusNoteType.CHR);
    private static SusNote FlkNote(string data) => ParseLaneWidth(data, SusNoteType.FLK);
    private static SusNote MineNote(string data) => ParseLaneWidth(data, SusNoteType.MNE);

    private static SusNote HoldNote(string data, SusNoteType type)
    {
        var n = ParseLaneWidth(data, type);
        if (data.Length >= 10) n.Duration = HexVal(data[6..10]);
        return n;
    }

    private static SusNote SlideNote(string data)
    {
        var n = new SusNote { Type = SusNoteType.SLD, Lane = 0, Width = 1 };
        if (data.Length >= 6) { n.Lane = HexVal(data[2..4]); n.Width = Math.Max(1, HexVal(data[4..6])); }
        if (data.Length >= 14) { n.Duration = HexVal(data[6..10]); n.EndLane = HexVal(data[10..12]); n.EndWidth = Math.Max(1, HexVal(data[12..14])); }
        return n;
    }

    private static SusNote AirNote(string data, SusNoteType type)
    {
        var n = ParseLaneWidth(data, type);
        if (data.Length >= 8) n.Target = data[6..8];
        return n;
    }

    private static SusNote ParseLaneWidth(string data, SusNoteType type)
    {
        int lane = 0, width = 1;
        if (data.Length >= 6) { lane = HexVal(data[2..4]); width = Math.Max(1, HexVal(data[4..6])); }
        return new SusNote { Type = type, Lane = lane, Width = width };
    }

    private static int HexVal(string hex) => int.TryParse(hex, NumberStyles.HexNumber, null, out var v) ? v : 0;
    private static double ParseDouble(string s, double def) => double.TryParse(s.Trim('"'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;
    private static string StripQuotes(string s) => s.Trim().Trim('"');
}
