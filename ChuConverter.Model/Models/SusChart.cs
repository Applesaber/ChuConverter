namespace ChuConverter.Models;

public class SusChart
{
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Designer { get; set; } = "";
    public int TicksPerBeat { get; set; } = 480;
    public double Bpm { get; set; } = 120.0;
    public List<SusNote> Notes { get; } = new();
}

public class SusNote
{
    public SusNoteType Type { get; set; }
    public int Measure { get; set; }
    public int Tick { get; set; }
    public int Lane { get; set; }
    public int Width { get; set; } = 1;
    public int Duration { get; set; }
    public int EndLane { get; set; }
    public int EndWidth { get; set; } = 1;
    public string Target { get; set; } = "N";
    public string Extra { get; set; } = "";
}

public enum SusNoteType
{
    TAP = 1,
    CHR = 2,
    FLK = 3,
    HLD = 5,
    SLD = 6,
    AIR = 7,
    AHD = 8,
    ADW = 9,
    MNE = 10,
}
