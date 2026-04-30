using ChuConverter.Models;

namespace ChuConverter;

public class SusToC2sConverter
{
    private const int C2sResolution = 384;

    public C2sChart Convert(SusChart sus)
    {
        var chart = new C2sChart { Resolution = C2sResolution, BpmDef = sus.Bpm };
        int ugcTpb = sus.TicksPerBeat > 0 ? sus.TicksPerBeat : 480;

        chart.BpmEvents.Add(new BpmEvent { Measure = 0, Offset = 0, Bpm = sus.Bpm });

        foreach (var n in sus.Notes)
        {
            int c2sOffset = ScaleDown(n.Tick, ugcTpb);
            var cn = new ChartNote
            {
                Type = MapType(n.Type),
                Measure = n.Measure,
                Offset = c2sOffset,
                Cell = n.Lane / 2,
                Width = Math.Max(1, n.Width / 2),
                HoldDuration = ScaleDown(n.Duration, ugcTpb),
                SlideDuration = ScaleDown(n.Duration, ugcTpb),
                EndCell = n.EndLane / 2,
                EndWidth = Math.Max(1, n.EndWidth / 2),
                TargetNote = n.Target,
                AirHoldDuration = ScaleDown(n.Duration, ugcTpb),
            };
            chart.Notes.Add(cn);
        }

        return chart;
    }

    private static int ScaleDown(int susTicks, int tpb) => (int)((long)susTicks * (C2sResolution / 4) / tpb);

    private static NoteType MapType(SusNoteType t) => t switch
    {
        SusNoteType.TAP => NoteType.TAP,
        SusNoteType.CHR => NoteType.CHR,
        SusNoteType.FLK => NoteType.FLK,
        SusNoteType.HLD => NoteType.HLD,
        SusNoteType.SLD => NoteType.SLD,
        SusNoteType.AIR => NoteType.AIR,
        SusNoteType.AHD => NoteType.AHD,
        SusNoteType.ADW => NoteType.ADW,
        SusNoteType.MNE => NoteType.MNE,
        _ => NoteType.TAP
    };
}
