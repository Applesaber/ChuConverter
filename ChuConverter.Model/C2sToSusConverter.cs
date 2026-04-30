using ChuConverter.Models;

namespace ChuConverter;

public class C2sToSusConverter
{
    private const int C2sResolution = 384;
    private const int SusTpb = 480;

    public SusChart Convert(C2sChart c2s, string title = "", string artist = "")
    {
        var sus = new SusChart
        {
            TicksPerBeat = SusTpb,
            Title = title,
            Artist = artist,
            Bpm = c2s.BpmEvents.Count > 0 ? c2s.BpmEvents[0].Bpm : c2s.BpmDef,
        };

        foreach (var n in c2s.Notes.OrderBy(n => n.TotalTick(C2sResolution)))
        {
            var sn = new SusNote
            {
                Type = MapType(n.Type),
                Measure = n.Measure,
                Tick = ScaleUp(n.Offset),
                Lane = n.Cell * 2,
                Width = Math.Min(32, n.Width * 2),
                Duration = ScaleUp(n.HoldDuration > 0 ? n.HoldDuration : n.SlideDuration > 0 ? n.SlideDuration : n.AirHoldDuration),
                EndLane = n.EndCell * 2,
                EndWidth = Math.Min(32, n.EndWidth * 2),
                Target = n.TargetNote.Length >= 2 ? n.TargetNote[..2] : n.TargetNote + "0",
                Extra = n.Extra,
            };
            sus.Notes.Add(sn);
        }

        return sus;
    }

    private static int ScaleUp(int c2sTicks) => c2sTicks * SusTpb / (C2sResolution / 4);

    private static SusNoteType MapType(NoteType t) => t switch
    {
        NoteType.TAP => SusNoteType.TAP,
        NoteType.CHR => SusNoteType.CHR,
        NoteType.FLK => SusNoteType.FLK,
        NoteType.HLD => SusNoteType.HLD,
        NoteType.SLD or NoteType.SLC => SusNoteType.SLD,
        NoteType.AIR or NoteType.AUR or NoteType.AUL => SusNoteType.AIR,
        NoteType.AHD => SusNoteType.AHD,
        NoteType.ADW or NoteType.ADR or NoteType.ADL => SusNoteType.ADW,
        NoteType.MNE => SusNoteType.MNE,
        _ => SusNoteType.TAP
    };
}
