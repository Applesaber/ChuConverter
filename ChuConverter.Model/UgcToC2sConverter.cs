using ChuConverter.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChuConverter;

public class UgcToC2sConverter
{
    private readonly ILogger<UgcToC2sConverter> _logger;

    public UgcToC2sConverter(ILogger<UgcToC2sConverter>? logger = null)
        => _logger = logger ?? NullLogger<UgcToC2sConverter>.Instance;

    /// <summary>C2S 每小节 tick 数</summary>
    private const int C2sResolution = 384;
    /// <summary>C2S 每拍 tick 数</summary>
    private const int C2sTicksPerBeat = C2sResolution / 4;
    private const int DefaultUgcTicksPerBeat = 480;

    public C2sChart Convert(UgcChart ugc)
    {
        var chart = new C2sChart
        {
            Resolution = C2sResolution,
            Version = "1.08.00\t1.08.00",
            Creator = ugc.Designer,
            MusicId = ParseMusicId(ugc.SongId),
            DifficultId = ParseDifficultyId(ugc.Difficulty),
            BpmDef = ugc.BpmEvents.Count > 0 ? ugc.BpmEvents[0].Bpm : 120.0
        };

        ConvertBpmEvents(ugc, chart);
        ConvertMetEvents(ugc, chart);
        ConvertSpeedEvents(ugc, chart);
        ConvertNotes(ugc, chart);
        ResolveAirTargets(chart);

        _logger.LogInformation("UGC→C2S 转换完成: {NoteCount} 个音符, {BpmCount} 个 BPM, {SflCount} 个 SFL",
            chart.Notes.Count, chart.BpmEvents.Count, chart.SflEvents.Count);

        return chart;
    }

    private static int ScaleTicks(int ugcTicks, int ugcTpb)
    {
        // UGC: ticks 按拍计量 (1 beat = ugcTpb ticks)
        // C2S: ticks 按小节计量 (1 measure = C2sResolution ticks)
        // 换算: c2s = ugc * (C2sResolution / 4) / ugcTpb = ugc * 96 / ugcTpb
        return (int)((long)ugcTicks * C2sTicksPerBeat / ugcTpb);
    }

    private void ConvertBpmEvents(UgcChart ugc, C2sChart chart)
    {
        int ugcTpb = ugc.TicksPerBeat > 0 ? ugc.TicksPerBeat : DefaultUgcTicksPerBeat;

        foreach (var ev in ugc.BpmEvents)
        {
            chart.BpmEvents.Add(new BpmEvent
            {
                Measure = ev.Measure,
                Offset = ScaleTicks(ev.Offset, ugcTpb),
                Bpm = ev.Bpm
            });
        }

        if (chart.BpmEvents.Count == 0)
        {
            chart.BpmEvents.Add(new BpmEvent { Measure = 0, Offset = 0, Bpm = 120.0 });
        }

        chart.BpmEvents.Sort((a, b) => a.TotalTick(C2sResolution).CompareTo(b.TotalTick(C2sResolution)));
    }

    private static void ConvertMetEvents(UgcChart ugc, C2sChart chart)
    {
        foreach (var beat in ugc.BeatEvents)
        {
            chart.MetEvents.Add(new MetEvent
            {
                Measure = beat.Measure,
                Offset = 0,
                Denominator = beat.Denominator,
                Numerator = beat.Numerator
            });
        }

        if (chart.MetEvents.Count == 0)
        {
            chart.MetEvents.Add(new MetEvent { Measure = 0, Offset = 0, Denominator = 4, Numerator = 4 });
        }
    }

    private void ConvertSpeedEvents(UgcChart ugc, C2sChart chart)
    {
        if (ugc.SpeedEvents.Count == 0)
            return;

        int ugcTpb = ugc.TicksPerBeat > 0 ? ugc.TicksPerBeat : DefaultUgcTicksPerBeat;
        var measureStarts = BuildMeasureStartTicks(ugc, ugcTpb);

        var absoluteEvents = new List<(int absTick, int measure, int offset, double multiplier)>();
        foreach (var spd in ugc.SpeedEvents)
        {
            // 默认倍率 1.0 无需输出 SFL
            if (Math.Abs(spd.Multiplier - 1.0) < 0.0001)
                continue;

            int abs = MeasureTickToAbsoluteTick(spd.Measure, spd.Offset, measureStarts);
            absoluteEvents.Add((abs, spd.Measure, spd.Offset, spd.Multiplier));
        }

        if (absoluteEvents.Count == 0)
            return;

        absoluteEvents.Sort((a, b) => a.absTick.CompareTo(b.absTick));

        int maxAbsTick = computeMaxAbsTick(ugc, measureStarts);

        for (int i = 0; i < absoluteEvents.Count; i++)
        {
            var current = absoluteEvents[i];
            int nextAbsTick = (i + 1 < absoluteEvents.Count)
                ? absoluteEvents[i + 1].absTick
                : maxAbsTick;
            int durationAbs = nextAbsTick - current.absTick;

            if (durationAbs <= 0)
                continue;

            int c2sDuration = ScaleTicks(durationAbs, ugcTpb);
            if (c2sDuration <= 0)
                continue;

            chart.SflEvents.Add(new SflEvent
            {
                Measure = current.measure,
                Offset = ScaleTicks(current.offset, ugcTpb),
                Duration = c2sDuration,
                Multiplier = current.multiplier
            });
        }

        chart.SflEvents.Sort((a, b) => a.TotalTick(C2sResolution).CompareTo(b.TotalTick(C2sResolution)));
    }

    private void ConvertNotes(UgcChart ugc, C2sChart chart)
    {
        int ugcTpb = ugc.TicksPerBeat > 0 ? ugc.TicksPerBeat : DefaultUgcTicksPerBeat;

        foreach (var n in ugc.Notes)
        {
            chart.Notes.Add(new ChartNote
            {
                Type = n.Type,
                Measure = n.Measure,
                Offset = ScaleTicks(n.Offset, ugcTpb),
                Cell = n.Cell,
                Width = n.Width,
                HoldDuration = ScaleTicks(n.HoldDuration, ugcTpb),
                SlideDuration = ScaleTicks(n.SlideDuration, ugcTpb),
                EndCell = n.EndCell,
                EndWidth = n.EndWidth,
                Extra = n.Extra,
                TargetNote = n.TargetNote,
                AirHoldDuration = ScaleTicks(n.AirHoldDuration, ugcTpb),
                StartHeight = n.StartHeight,
                TargetHeight = n.TargetHeight,
                NoteColor = n.NoteColor
            });
        }
    }

    private static int computeMaxAbsTick(UgcChart ugc, int[] measureStarts)
    {
        int maxM = 0;
        foreach (var n in ugc.Notes)
            maxM = Math.Max(maxM, n.Measure);
        foreach (var b in ugc.BpmEvents)
            maxM = Math.Max(maxM, b.Measure);

        maxM += 2;
        return maxM < measureStarts.Length
            ? measureStarts[maxM]
            : measureStarts[^1] + 1920;
    }

    private static int[] BuildMeasureStartTicks(UgcChart ugc, int ugcTpb)
    {
        var beatEvents = new List<UgcBeatEvent>(ugc.BeatEvents);
        if (beatEvents.Count == 0)
        {
            beatEvents.Add(new UgcBeatEvent { Measure = 0, Numerator = 4, Denominator = 4 });
        }

        beatEvents.Sort((a, b) => a.Measure.CompareTo(b.Measure));

        int maxMeasure = 0;
        foreach (var n in ugc.Notes)
            maxMeasure = Math.Max(maxMeasure, n.Measure);
        foreach (var b in ugc.BpmEvents)
            maxMeasure = Math.Max(maxMeasure, b.Measure);
        foreach (var s in ugc.SpeedEvents)
            maxMeasure = Math.Max(maxMeasure, s.Measure);

        maxMeasure += 16;
        var starts = new int[Math.Max(2, maxMeasure + 2)];

        int evIdx = 0;
        int num = beatEvents[0].Numerator;
        int den = beatEvents[0].Denominator;

        starts[0] = 0;
        for (int m = 0; m < starts.Length - 1; m++)
        {
            while (evIdx + 1 < beatEvents.Count && beatEvents[evIdx + 1].Measure <= m)
            {
                evIdx++;
                num = beatEvents[evIdx].Numerator;
                den = beatEvents[evIdx].Denominator;
            }

            int ticksPerMeasure = ComputeMeasureTicks(ugcTpb, num, den);
            starts[m + 1] = starts[m] + ticksPerMeasure;
        }

        return starts;
    }

    private static int ComputeMeasureTicks(int ticksPerBeat, int numerator, int denominator)
    {
        double v = ticksPerBeat * numerator * 4.0 / Math.Max(1, denominator);
        return Math.Max(1, (int)Math.Round(v));
    }

    private static int MeasureTickToAbsoluteTick(int measure, int tickInMeasure, int[] measureStarts)
    {
        if (measure < 0)
            measure = 0;

        if (measure >= measureStarts.Length - 1)
            return measureStarts[^1] + tickInMeasure;

        return measureStarts[measure] + tickInMeasure;
    }

    private static int ParseMusicId(string songId)
        => int.TryParse(songId, out var id) ? id : 0;

    private static int ParseDifficultyId(string difficulty) => difficulty.ToUpperInvariant() switch
    {
        "BASIC" => 0,
        "ADVANCED" => 1,
        "EXPERT" => 2,
        "MASTER" => 3,
        "ULTIMA" => 4,
        "WORLD'S END" => 5,
        _ => int.TryParse(difficulty, out var id) ? id : 0
    };

    private static void ResolveAirTargets(C2sChart chart)
    {
        var notes = chart.Notes.OrderBy(n => n.TotalTick(C2sResolution)).ToList();

        foreach (var note in notes)
        {
            if (note.TargetNote != "N") continue;

            var tick = note.TotalTick(C2sResolution);
            // 在当前位置之前找到最近的非空气音符（跨小节搜索）
            var target = notes
                .Where(n => n.TotalTick(C2sResolution) <= tick && !isAirType(n.Type))
                .MaxBy(n => n.TotalTick(C2sResolution));

            if (target != null)
                note.TargetNote = TypeCode(target.Type);
        }
    }

    private static bool isAirType(NoteType t) => t switch
    {
        NoteType.AIR or NoteType.AUR or NoteType.AUL
          or NoteType.AHD or NoteType.ADW or NoteType.ADR or NoteType.ADL
          or NoteType.ALD or NoteType.ASD => true,
        _ => false
    };

    private static string TypeCode(NoteType t) => t switch
    {
        NoteType.TAP => "TAP",
        NoteType.CHR => "CHR",
        NoteType.HLD => "HLD",
        NoteType.HXD => "HXD",
        NoteType.SLD => "SLD",
        NoteType.SLC => "SLC",
        NoteType.SXD => "SXD",
        NoteType.SXC => "SXC",
        NoteType.FLK => "FLK",
        NoteType.MNE => "MNE",
        _ => "TAP"
    };
}
