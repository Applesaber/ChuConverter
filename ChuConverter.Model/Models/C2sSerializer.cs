using System.Globalization;
using System.Text;

namespace ChuConverter.Models;

public static class C2sSerializer
{
    public static string Serialize(C2sChart chart)
    {
        var sb = new StringBuilder();

        WriteHeader(chart, sb);
        sb.AppendLine();

        WriteTiming(chart, sb);
        sb.AppendLine();

        WriteNotes(chart, sb);
        sb.AppendLine();

        WriteStats(chart, sb);

        return sb.ToString();
    }

    private static void WriteHeader(C2sChart chart, StringBuilder sb)
    {
        sb.AppendLine($"VERSION\t{chart.Version}");
        sb.AppendLine($"MUSIC\t{chart.MusicId}");
        sb.AppendLine("SEQUENCEID\t0");
        sb.AppendLine($"DIFFICULT\t{chart.DifficultId:D2}");
        sb.AppendLine("LEVEL\t0.0");
        sb.AppendLine($"CREATOR\t{chart.Creator}");

        var (start, mode, high, low) = ComputeBpmDef(chart);
        sb.AppendLine($"BPM_DEF\t{Fmt(start)}\t{Fmt(mode)}\t{Fmt(high)}\t{Fmt(low)}");

        sb.AppendLine($"MET_DEF\t4\t4");
        sb.AppendLine($"RESOLUTION\t{chart.Resolution}");
        sb.AppendLine($"CLK_DEF\t{chart.Resolution}");
        sb.AppendLine("PROGJUDGE_BPM\t240.000");
        sb.AppendLine("PROGJUDGE_AER\t  0.999");
        sb.AppendLine("TUTORIAL\t0");
    }

    private static (double start, double mode, double high, double low) ComputeBpmDef(C2sChart chart)
    {
        double start = chart.BpmEvents.Count > 0
            ? chart.BpmEvents.OrderBy(e => e.TotalTick(chart.Resolution)).First().Bpm
            : chart.BpmDef;

        double high = start;
        double low = start;
        foreach (var b in chart.BpmEvents)
        {
            high = Math.Max(high, b.Bpm);
            low = Math.Min(low, b.Bpm);
        }

        return (start, start, high, low);
    }

    private static void WriteTiming(C2sChart chart, StringBuilder sb)
    {
        foreach (var ev in chart.BpmEvents.OrderBy(e => e.TotalTick(chart.Resolution)))
        {
            sb.AppendLine($"BPM\t{ev.Measure}\t{ev.Offset}\t{Fmt(ev.Bpm)}");
        }

        foreach (var ev in chart.MetEvents.OrderBy(e => e.TotalTick(chart.Resolution)))
        {
            sb.AppendLine($"MET\t{ev.Measure}\t{ev.Offset}\t{ev.Denominator}\t{ev.Numerator}");
        }

        foreach (var ev in chart.SflEvents.OrderBy(e => e.TotalTick(chart.Resolution)))
        {
            sb.AppendLine($"SFL\t{ev.Measure}\t{ev.Offset}\t{ev.Duration}\t{Mlt(ev.Multiplier)}");
        }
    }

    private static void WriteNotes(C2sChart chart, StringBuilder sb)
    {
        foreach (var n in chart.Notes.OrderBy(n => n.TotalTick(chart.Resolution)))
        {
            sb.AppendLine(FormatNote(n));
        }
    }

    private static string FormatNote(ChartNote n)
    {
        return n.Type switch
        {
            NoteType.TAP => $"TAP\t{n.Measure}\t{n.Offset}\t{n.Cell}\t{n.Width}",
            NoteType.CHR => $"CHR\t{n.Measure}\t{n.Offset}\t{n.Cell}\t{n.Width}\t{n.Extra}",
            NoteType.HLD or NoteType.HXD => $"{(n.Type == NoteType.HXD ? "HXD" : "HLD")}\t{n.Measure}\t{n.Offset}\t{n.Cell}\t{n.Width}\t{n.HoldDuration}",
            NoteType.SLD or NoteType.SLC or NoteType.SXD or NoteType.SXC =>
                $"{TypeCode(n.Type)}\t{n.Measure}\t{n.Offset}\t{n.Cell}\t{n.Width}\t{n.SlideDuration}\t{n.EndCell}\t{n.EndWidth}",
            NoteType.FLK => $"FLK\t{n.Measure}\t{n.Offset}\t{n.Cell}\t{n.Width}\t{n.Extra}",
            NoteType.AIR or NoteType.AUR or NoteType.AUL or NoteType.ADW or NoteType.ADR or NoteType.ADL =>
                $"{n.Type}\t{n.Measure}\t{n.Offset}\t{n.Cell}\t{n.Width}\t{n.TargetNote}",
            NoteType.AHD => $"AHD\t{n.Measure}\t{n.Offset}\t{n.Cell}\t{n.Width}\t{n.TargetNote}\t{n.AirHoldDuration}",
            NoteType.ALD or NoteType.ASD =>
                $"{n.Type}\t{n.Measure}\t{n.Offset}\t{n.StartHeight}\t{n.SlideDuration}\t{n.EndCell}\t{n.EndWidth}\t{n.TargetHeight}\t{n.NoteColor}",
            NoteType.MNE => $"MNE\t{n.Measure}\t{n.Offset}\t{n.Cell}\t{n.Width}",
            _ => $"TAP\t{n.Measure}\t{n.Offset}\t{n.Cell}\t{n.Width}"
        };
    }

    private static string TypeCode(NoteType t) => t switch
    {
        NoteType.SLD => "SLD",
        NoteType.SLC => "SLC",
        NoteType.SXD => "SXD",
        NoteType.SXC => "SXC",
        _ => "SLD"
    };

    private static void WriteStats(C2sChart chart, StringBuilder sb)
    {
        int tap = 0, chr = 0, flk = 0, mne = 0;
        int hld = 0, sld = 0, air = 0, ahd = 0, aac = 0;
        long lenHld = 0, lenSld = 0, lenAhd = 0;
        int chrUp = 0, judgeAll = 0;

        foreach (var n in chart.Notes)
        {
            switch (n.Type)
            {
                case NoteType.TAP: tap++; judgeAll++; break;
                case NoteType.CHR: chr++; judgeAll++; chrUp += n.Extra == "UP" ? 1 : 0; break;
                case NoteType.FLK: flk++; judgeAll++; break;
                case NoteType.MNE: mne++; judgeAll++; break;
                case NoteType.HLD or NoteType.HXD:
                    hld++; lenHld += n.HoldDuration; judgeAll += 3; break;
                case NoteType.SLD or NoteType.SLC or NoteType.SXD or NoteType.SXC:
                    sld++; lenSld += n.SlideDuration; judgeAll += 2; break;
                case NoteType.AIR or NoteType.AUR or NoteType.AUL
                  or NoteType.ADW or NoteType.ADR or NoteType.ADL:
                    air++; aac++; judgeAll += 2; break;
                case NoteType.AHD:
                    ahd++; aac++; lenAhd += n.AirHoldDuration; judgeAll += 3; break;
                case NoteType.ALD or NoteType.ASD:
                    aac++; break;
            }
        }

        int totalNotes = tap + chr + flk + mne + hld + sld + air + ahd;

        // T_LEN uses milliseconds (per official Chunithm-Research docs)
        // ms = ticks * 625 / bpm (where bpm = first BPM event's value)
        double bpm = chart.BpmEvents.Count > 0 ? chart.BpmEvents[0].Bpm : chart.BpmDef;
        int lenHldMs = (int)(lenHld * 625.0 / bpm);
        int lenSldMs = (int)(lenSld * 625.0 / bpm);
        int lenAhdMs = (int)(lenAhd * 625.0 / bpm);

        sb.AppendLine($"T_REC_TAP\t{tap}");
        sb.AppendLine($"T_REC_CHR\t{chr}");
        sb.AppendLine($"T_REC_FLK\t{flk}");
        sb.AppendLine($"T_REC_MNE\t{mne}");
        sb.AppendLine($"T_REC_HLD\t{hld}");
        sb.AppendLine($"T_REC_SLD\t{sld}");
        sb.AppendLine($"T_REC_AIR\t{air}");
        sb.AppendLine($"T_REC_AHD\t{ahd}");
        sb.AppendLine($"T_REC_ALL\t{totalNotes}");
        sb.AppendLine($"T_NOTE_TAP\t{tap}");
        sb.AppendLine($"T_NOTE_CHR\t{chr}");
        sb.AppendLine($"T_NOTE_FLK\t{flk}");
        sb.AppendLine($"T_NOTE_MNE\t{mne}");
        sb.AppendLine($"T_NOTE_HLD\t{hld}");
        sb.AppendLine($"T_NOTE_SLD\t{sld}");
        sb.AppendLine($"T_NOTE_AIR\t{air}");
        sb.AppendLine($"T_NOTE_AHD\t{ahd}");
        sb.AppendLine($"T_NOTE_ALL\t{totalNotes}");
        sb.AppendLine($"T_NUM_TAP\t{tap + 2 * (air + ahd)}");
        sb.AppendLine($"T_NUM_CHR\t{chr}");
        sb.AppendLine($"T_NUM_FLK\t{flk}");
        sb.AppendLine($"T_NUM_MNE\t{mne}");
        sb.AppendLine($"T_NUM_HLD\t{hld}");
        sb.AppendLine($"T_NUM_SLD\t{sld}");
        sb.AppendLine($"T_NUM_AIR\t{air + ahd}");
        sb.AppendLine($"T_NUM_AHD\t{ahd}");
        sb.AppendLine($"T_NUM_AAC\t{ahd}");
        sb.AppendLine($"T_CHRTYPE_UP\t{chrUp}");
        sb.AppendLine($"T_CHRTYPE_DW\t0");
        sb.AppendLine($"T_CHRTYPE_CE\t0");
        sb.AppendLine($"T_LEN_HLD\t{lenHldMs}");
        sb.AppendLine($"T_LEN_SLD\t{lenSldMs}");
        sb.AppendLine($"T_LEN_AHD\t{lenAhdMs}");
        sb.AppendLine($"T_LEN_ALL\t{lenHldMs + lenSldMs + lenAhdMs}");
        sb.AppendLine($"T_JUDGE_TAP\t{judgeAll}");
        sb.AppendLine($"T_JUDGE_HLD\t{hld * 3}");
        sb.AppendLine($"T_JUDGE_SLD\t{sld * 2}");
        sb.AppendLine($"T_JUDGE_AIR\t{air * 2 + ahd * 3}");
        sb.AppendLine($"T_JUDGE_FLK\t{flk}");
        sb.AppendLine($"T_JUDGE_ALL\t{judgeAll + hld * 2 + sld * 1 + air * 1 + ahd * 2}");

        if (chart.Notes.Count > 0)
        {
            var sorted = chart.Notes.OrderBy(n => n.TotalTick(chart.Resolution)).ToList();
            int firstRes = sorted[0].TotalTick(chart.Resolution);
            var lastNote = sorted[^1];
            int lastRes = lastNote.TotalTick(chart.Resolution);
            int lastDur = lastNote.HoldDuration > 0 ? lastNote.HoldDuration
                : lastNote.SlideDuration > 0 ? lastNote.SlideDuration
                : lastNote.AirHoldDuration > 0 ? lastNote.AirHoldDuration : 0;
            int finalRes = lastRes + lastDur;

            int firstMsec = (int)(firstRes * 625.0 / bpm);
            int finalMsec = (int)(finalRes * 625.0 / bpm);

            sb.AppendLine($"T_FIRST_MSEC\t{firstMsec}");
            sb.AppendLine($"T_FIRST_RES\t{firstRes}");
            sb.AppendLine($"T_FINAL_MSEC\t{finalMsec}");
            sb.AppendLine($"T_FINAL_RES\t{finalRes}");

            int totalSpan = finalRes;
            int prevCumulative = 0;
            for (int seg = 0; seg <= 95; seg += 5)
            {
                int endTick = (int)(totalSpan * (seg + 5L) / 100);
                int cumulative = 0;
                foreach (var n in sorted)
                {
                    if (n.TotalTick(chart.Resolution) >= endTick) break;
                    cumulative += JudgmentWeight(n.Type);
                }
                sb.AppendLine($"T_PROG_{seg:D2}\t{cumulative - prevCumulative}");
                prevCumulative = cumulative;
            }
        }
        else
        {
            sb.AppendLine("T_FIRST_MSEC\t0");
            sb.AppendLine("T_FIRST_RES\t0");
        }

        sb.AppendLine();
    }

    private static int JudgmentWeight(NoteType t) => t switch
    {
        NoteType.TAP => 1,
        NoteType.CHR => 1,
        NoteType.FLK => 1,
        NoteType.MNE => 1,
        NoteType.HLD or NoteType.HXD => 3,
        NoteType.SLD or NoteType.SLC or NoteType.SXD or NoteType.SXC => 2,
        NoteType.AIR or NoteType.AUR or NoteType.AUL
          or NoteType.ADW or NoteType.ADR or NoteType.ADL => 2,
        NoteType.AHD => 3,
        _ => 0
    };

    private static string Fmt(double v)
        => v.ToString("0.000", CultureInfo.InvariantCulture);

    private static string Mlt(double v)
        => v.ToString("0.000000", CultureInfo.InvariantCulture);
}
