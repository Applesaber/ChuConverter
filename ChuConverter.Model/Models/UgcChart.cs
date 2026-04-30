using System.Collections.Generic;

namespace ChuConverter.Models;

public class UgcChart
{
    public string Version { get; set; } = "";
    public string Designer { get; set; } = "";
    public string SongName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Difficulty { get; set; } = "";
    public int Level { get; set; }
    public double Constant { get; set; }
    public string SongId { get; set; } = "";
    public int TicksPerBeat { get; set; } = 480;
    public List<UgcBeatEvent> BeatEvents { get; } = new();
    public List<UgcBpmEvent> BpmEvents { get; } = new();
    public List<UgcSpeedEvent> SpeedEvents { get; } = new();
    public List<UgcNote> Notes { get; } = new();
    public double TotalDuration { get; set; }
}

public class UgcBeatEvent
{
    public int Measure { get; set; }
    public int Numerator { get; set; }
    public int Denominator { get; set; }
}

public class UgcBpmEvent
{
    public int Measure { get; set; }
    public int Offset { get; set; }
    public double Bpm { get; set; }
}

public class UgcSpeedEvent
{
    public int Measure { get; set; }
    public int Offset { get; set; }
    public double Multiplier { get; set; }
}

public class UgcNote
{
    public NoteType Type { get; set; }
    public int Measure { get; set; }
    public int Offset { get; set; }
    public int Cell { get; set; }
    public int Width { get; set; }
    public int HoldDuration { get; set; }
    public int SlideDuration { get; set; }
    public int EndCell { get; set; }
    public int EndWidth { get; set; }
    public string Extra { get; set; } = "";
    public string TargetNote { get; set; } = "";
    public int AirHoldDuration { get; set; }
    public int StartHeight { get; set; }
    public int TargetHeight { get; set; }
    public string NoteColor { get; set; } = "";
    public double Time { get; set; }
    public double EndTime { get; set; }
}
