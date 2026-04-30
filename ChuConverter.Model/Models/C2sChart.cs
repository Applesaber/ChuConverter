using System.Globalization;

namespace ChuConverter.Models;

public enum NoteType
{
    TAP, CHR, HLD, HXD,
    SLD, SLC, SXD, SXC,
    FLK,
    AIR, AUR, AUL,
    AHD,
    ADW, ADR, ADL,
    ALD, ASD,
    MNE
}

public class ChartNote
{
    public NoteType Type { get; init; }
    public int Measure { get; init; }
    public int Offset { get; init; }
    public int Cell { get; init; }
    public int Width { get; init; }

    public double Time { get; set; }
    public double EndTime { get; set; }

    public int HoldDuration { get; init; }
    public int SlideDuration { get; init; }
    public int EndCell { get; init; }
    public int EndWidth { get; init; }
    public string Extra { get; init; } = "";
    public string TargetNote { get; set; } = "";
    public int AirHoldDuration { get; init; }
    public int StartHeight { get; init; }
    public int TargetHeight { get; init; }
    public string NoteColor { get; init; } = "";

    public int TotalTick(int resolution) => Measure * resolution + Offset;

    public bool IsSlide => Type is NoteType.SLD or NoteType.SLC or NoteType.SXD or NoteType.SXC;
    public bool IsAirAction => Type is NoteType.AIR or NoteType.AUR or NoteType.AUL
                                    or NoteType.ADW or NoteType.ADR or NoteType.ADL;
    public bool IsHold => Type is NoteType.HLD or NoteType.HXD;
}

public class BpmEvent
{
    public int Measure { get; init; }
    public int Offset { get; init; }
    public double Bpm { get; init; }
    public double Time { get; set; }

    public int TotalTick(int resolution) => Measure * resolution + Offset;
}

public class SflEvent
{
    public int Measure { get; init; }
    public int Offset { get; init; }
    public int Duration { get; init; }
    public double Multiplier { get; init; }
    public double Time { get; set; }
    public double EndTime { get; set; }

    public int TotalTick(int resolution) => Measure * resolution + Offset;
}

public class MetEvent
{
    public int Measure { get; init; }
    public int Offset { get; init; }
    public int Denominator { get; init; }
    public int Numerator { get; init; }

    public int TotalTick(int resolution) => Measure * resolution + Offset;
}

public class C2sChart
{
    public string Version { get; set; } = "1.08.00\t1.08.00";
    public int MusicId { get; set; }
    public int DifficultId { get; set; }
    public string Creator { get; set; } = "";
    public int Resolution { get; set; } = 384;
    public double BpmDef { get; set; }

    public List<BpmEvent> BpmEvents { get; } = [];
    public List<MetEvent> MetEvents { get; } = [];
    public List<SflEvent> SflEvents { get; } = [];
    public List<ChartNote> Notes { get; } = [];

    public double TotalDuration { get; set; }

    private static int Int(string[] p, int i, int def = 0)
        => i < p.Length && int.TryParse(p[i], out var v) ? v : def;

    private static double Dbl(string[] p, int i, double def = 0)
        => i < p.Length && double.TryParse(p[i], CultureInfo.InvariantCulture, out var v) ? v : def;

    private static string Str(string[] p, int i)
        => i < p.Length ? p[i] : "";
}
