using ChuConverter.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChuConverter;

public class C2sToUgcConverter
{
    private readonly ILogger<C2sToUgcConverter> _logger;

    public C2sToUgcConverter(ILogger<C2sToUgcConverter>? logger = null)
        => _logger = logger ?? NullLogger<C2sToUgcConverter>.Instance;
    private const int UgcTicksPerBeat = 480;
    private const int C2sResolution = 384;
    private const int ScaleFactor = UgcTicksPerBeat / (C2sResolution / 4);

    public UgcChart Convert(C2sChart c2s, MusicXmlData musicXml)
    {
        _logger.LogInformation("开始 C2S→UGC 转换: 曲名={Title}, 音符={Count}",
            musicXml.Title, c2s.Notes.Count);

        var fumen = musicXml.GetFumen(c2s.DifficultId);

        var ugc = new UgcChart
        {
            Version = "6",
            TicksPerBeat = UgcTicksPerBeat,
            Difficulty = MapDifficultyId(c2s.DifficultId),
            SongId = c2s.MusicId.ToString(),
            Designer = string.IsNullOrEmpty(fumen?.NotesDesigner) ? c2s.Creator : fumen.NotesDesigner,
            Title = musicXml.Title,
            Artist = musicXml.Artist,
            Level = fumen?.Level ?? 0,
            Constant = fumen?.Constant ?? 0.0,
        };

        ConvertBpmEvents(c2s, ugc);
        ConvertMetEvents(c2s, ugc);
        ConvertNotes(c2s, ugc);

        _logger.LogInformation("C2S→UGC 转换完成: {NoteCount} 个音符, @TICKS={Ticks}",
            ugc.Notes.Count, ugc.TicksPerBeat);

        return ugc;
    }

    private static int ScaleUp(int c2sTicks) => c2sTicks * ScaleFactor;

    private void ConvertBpmEvents(C2sChart c2s, UgcChart ugc)
    {
        foreach (var ev in c2s.BpmEvents)
            ugc.BpmEvents.Add(new UgcBpmEvent { Measure = ev.Measure, Offset = ScaleUp(ev.Offset), Bpm = ev.Bpm });

        if (ugc.BpmEvents.Count == 0)
            ugc.BpmEvents.Add(new UgcBpmEvent { Measure = 0, Offset = 0, Bpm = 120.0 });
    }

    private void ConvertMetEvents(C2sChart c2s, UgcChart ugc)
    {
        foreach (var ev in c2s.MetEvents)
            ugc.BeatEvents.Add(new UgcBeatEvent { Measure = ev.Measure, Numerator = ev.Numerator, Denominator = ev.Denominator });

        if (ugc.BeatEvents.Count == 0)
            ugc.BeatEvents.Add(new UgcBeatEvent { Measure = 0, Numerator = 4, Denominator = 4 });
    }

    private void ConvertNotes(C2sChart c2s, UgcChart ugc)
    {
        var sorted = c2s.Notes.OrderBy(n => n.TotalTick(C2sResolution)).ToList();
        int measureStartsAbs = sorted[0].TotalTick(C2sResolution);

        for (int i = 0; i < sorted.Count; i++)
        {
            var n = sorted[i];

            bool isSlideSegment = n.Type is NoteType.SLC or NoteType.SXC or NoteType.SXD;

            if (n.IsSlide && !isSlideSegment)
            {
                var (ugcNote, totalUgcDur) = BuildSlideChain(sorted, i);
                ugcNote.TargetNote = "N";
                ugc.Notes.Add(ugcNote);

                // 只添加 start line，chain 在序列化时处理
                int skipCount = 0;
                int endTick = n.TotalTick(C2sResolution) + n.SlideDuration;
                for (int j = i + 1; j < sorted.Count; j++)
                {
                    if (sorted[j].IsSlide && sorted[j].TotalTick(C2sResolution) + sorted[j].SlideDuration <= endTick + 2)
                        skipCount++;
                    else
                        break;
                }

                i += skipCount;
                continue;
            }

            if (isSlideSegment)
                continue;

            var note = new UgcNote
            {
                Type = n.Type,
                Measure = n.Measure,
                Offset = ScaleUp(n.Offset),
                Cell = n.Cell,
                Width = n.Width,
                Extra = n.Extra,
                TargetNote = IsAirType(n.Type) ? "N" : n.TargetNote,
            };

            if (n.Type == NoteType.HLD)
            {
                note.HoldDuration = ScaleUp(n.HoldDuration);
            }
            else if (n.Type == NoteType.AHD)
            {
                note.AirHoldDuration = ScaleUp(n.AirHoldDuration);
            }

            ugc.Notes.Add(note);
        }
    }

    private static (UgcNote note, int totalDuration) BuildSlideChain(List<ChartNote> sorted, int startIdx)
    {
        var start = sorted[startIdx];
        int startAbs = start.TotalTick(C2sResolution);

        var chainEnd = start;
        int endAbs = startAbs + start.SlideDuration;
        int skip = 0;

        for (int j = startIdx + 1; j < sorted.Count; j++)
        {
            var seg = sorted[j];
            if (!seg.IsSlide) break;
            int segTick = seg.TotalTick(C2sResolution);
            if (segTick > endAbs + 2) break;

            chainEnd = seg;
            endAbs = segTick + seg.SlideDuration;
            skip++;
        }

        var note = new UgcNote
        {
            Type = NoteType.SLD,
            Measure = start.Measure,
            Offset = ScaleUp(start.Offset),
            Cell = start.Cell,
            Width = start.Width,
            SlideDuration = ScaleUp(endAbs - startAbs),
            EndCell = chainEnd.EndCell,
            EndWidth = chainEnd.EndWidth,
        };

        return (note, note.SlideDuration);
    }

    private static bool IsAirType(NoteType t) => t switch
    {
        NoteType.AIR or NoteType.AUR or NoteType.AUL
          or NoteType.AHD or NoteType.ADW or NoteType.ADR or NoteType.ADL
          or NoteType.ALD or NoteType.ASD => true,
        _ => false
    };

    private static string MapDifficultyId(int id) => id switch
    {
        0 => "BASIC", 1 => "ADVANCED", 2 => "EXPERT", 3 => "MASTER", 4 => "ULTIMA", _ => "0"
    };
}
