using System;
using System.IO;
using System.Linq;
using ChuConverter;
using ChuConverter.Models;
using Xunit;

namespace ChuConverter.Tests;

public class UgcToC2sConverterTests
{
    private static string ExampleDir => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "examples");
    private static string SamplePath => Path.Combine(ExampleDir, "0003_bas.ugc");

    [Fact]
    public void CanConvertBasicUgcToC2s()
    {
        if (!File.Exists(SamplePath))
            throw new SkipException($"Test file not found: {SamplePath}");

        var ugc = UgcParser.Parse(File.ReadAllText(SamplePath));
        var converter = new UgcToC2sConverter();
        var c2s = converter.Convert(ugc);

        Assert.NotNull(c2s);
        Assert.Equal(384, c2s.Resolution);
        Assert.Equal("ロシェ＠ペンギン", c2s.Creator);
        Assert.NotEmpty(c2s.BpmEvents);
        Assert.NotEmpty(c2s.Notes);
    }

    [Fact]
    public void ConvertedNotesHaveCorrectMeasureAndType()
    {
        if (!File.Exists(SamplePath))
            throw new SkipException($"Test file not found: {SamplePath}");

        var ugc = UgcParser.Parse(File.ReadAllText(SamplePath));
        var converter = new UgcToC2sConverter();
        var c2s = converter.Convert(ugc);

        Assert.Equal(ugc.Notes.Count, c2s.Notes.Count);

        var firstNote = c2s.Notes.OrderBy(n => n.TotalTick(384)).First();
        Assert.Equal(5, firstNote.Measure);
        Assert.Equal(0, firstNote.Offset);
        Assert.Equal(NoteType.TAP, firstNote.Type);
        Assert.Equal(8, firstNote.Cell);
        Assert.Equal(4, firstNote.Width);
    }

    [Fact]
    public void ConvertedBpmEventsHaveCorrectValues()
    {
        if (!File.Exists(SamplePath))
            throw new SkipException($"Test file not found: {SamplePath}");

        var ugc = UgcParser.Parse(File.ReadAllText(SamplePath));
        var converter = new UgcToC2sConverter();
        var c2s = converter.Convert(ugc);

        var firstBpm = c2s.BpmEvents.OrderBy(b => b.TotalTick(384)).First();
        Assert.Equal(0, firstBpm.Measure);
        Assert.Equal(0, firstBpm.Offset);
        Assert.Equal(170.0, firstBpm.Bpm, 0.001);
    }

    [Fact]
    public void BpmDefMatchesFirstBpmEvent()
    {
        if (!File.Exists(SamplePath))
            throw new SkipException($"Test file not found: {SamplePath}");

        var ugc = UgcParser.Parse(File.ReadAllText(SamplePath));
        var converter = new UgcToC2sConverter();
        var c2s = converter.Convert(ugc);

        Assert.Equal(170.0, c2s.BpmDef, 0.001);
        Assert.Equal(c2s.BpmEvents[0].Bpm, c2s.BpmDef, 0.001);
    }

    [Fact]
    public void SerializationProducesValidOutput()
    {
        if (!File.Exists(SamplePath))
            throw new SkipException($"Test file not found: {SamplePath}");

        var ugc = UgcParser.Parse(File.ReadAllText(SamplePath));
        var converter = new UgcToC2sConverter();
        var c2s = converter.Convert(ugc);
        var text = C2sSerializer.Serialize(c2s);

        Assert.NotNull(text);
        Assert.Contains("VERSION", text);
        Assert.Contains("RESOLUTION\t384", text);
        Assert.Contains("BPM_DEF", text);
        Assert.Contains("BPM\t", text);
        Assert.Contains("TAP\t", text);
        Assert.Contains("MET\t0\t0\t4\t4", text);
        Assert.DoesNotContain("SFL\t", text);
    }

    [Fact]
    public void MetEventIsGenerated()
    {
        if (!File.Exists(SamplePath))
            throw new SkipException($"Test file not found: {SamplePath}");

        var ugc = UgcParser.Parse(File.ReadAllText(SamplePath));
        var converter = new UgcToC2sConverter();
        var c2s = converter.Convert(ugc);

        Assert.NotEmpty(c2s.MetEvents);
        var firstMet = c2s.MetEvents.First();
        Assert.Equal(0, firstMet.Measure);
        Assert.Equal(4, firstMet.Numerator);
        Assert.Equal(4, firstMet.Denominator);
    }

    [Fact]
    public void HoldDurationIsCorrectlyScaled()
    {
        if (!File.Exists(SamplePath))
            throw new SkipException($"Test file not found: {SamplePath}");

        var ugc = UgcParser.Parse(File.ReadAllText(SamplePath));
        var converter = new UgcToC2sConverter();
        var c2s = converter.Convert(ugc);

        // UGC duration 960 * (96/480) = C2S 192
        var holdNote = c2s.Notes.FirstOrDefault(n => n.Type == NoteType.HLD && n.Measure == 12);
        Assert.NotNull(holdNote);
        Assert.Equal(192, holdNote.HoldDuration);
    }

    [Fact]
    public void TapOffsetIsCorrectlyScaled()
    {
        if (!File.Exists(SamplePath))
            throw new SkipException($"Test file not found: {SamplePath}");

        var ugc = UgcParser.Parse(File.ReadAllText(SamplePath));
        var converter = new UgcToC2sConverter();
        var c2s = converter.Convert(ugc);

        var noteAtBeat1 = c2s.Notes.FirstOrDefault(n => n.Type == NoteType.TAP && n.Measure == 5 && n.Offset == 0);
        Assert.NotNull(noteAtBeat1);

        // tick 480 (第2拍) → offset = 480 * 96 / 480 = 96
        var noteAtBeat2 = c2s.Notes.FirstOrDefault(n => n.Type == NoteType.TAP && n.Measure == 5 && n.Offset == 96);
        Assert.NotNull(noteAtBeat2);
    }

    [Fact]
    public void DumpSerializedOutputToFile()
    {
        if (!File.Exists(SamplePath))
            throw new SkipException($"Test file not found: {SamplePath}");

        var ugc = UgcParser.Parse(File.ReadAllText(SamplePath));
        var converter = new UgcToC2sConverter();
        var c2s = converter.Convert(ugc);
        var text = C2sSerializer.Serialize(c2s);

        var outPath = Path.Combine(ExampleDir, "0003_bas_output.c2s");
        File.WriteAllText(outPath, text);

        Assert.True(File.Exists(outPath));
        var firstLine = File.ReadLines(outPath).First();
        Assert.StartsWith("VERSION", firstLine);
    }
}
