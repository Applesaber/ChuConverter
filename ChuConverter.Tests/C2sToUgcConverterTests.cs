using System;
using System.IO;
using System.Linq;
using ChuConverter;
using ChuConverter.Models;
using Xunit;

namespace ChuConverter.Tests;

public class C2sToUgcConverterTests
{
    private static string ExampleDir => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "examples");
    private static string OfficialC2s => Path.Combine(ExampleDir, "0003_00.c2s");
    private static string MusicXmlPath => Path.Combine(ExampleDir, "Music.xml");

    private static MusicXmlData LoadMusicXml()
    {
        if (!File.Exists(MusicXmlPath))
            throw new SkipException($"Music.xml not found: {MusicXmlPath}");
        return MusicXmlParser.Parse(MusicXmlPath);
    }

    private static C2sChart LoadC2s()
    {
        if (!File.Exists(OfficialC2s))
            throw new SkipException($"Official C2S not found: {OfficialC2s}");
        return C2sParser.Parse(File.ReadAllText(OfficialC2s));
    }

    [Fact]
    public void CanConvertBaseC2sToUgc()
    {
        var c2s = LoadC2s();
        var xml = LoadMusicXml();
        var ugc = new C2sToUgcConverter().Convert(c2s, xml);

        Assert.NotNull(ugc);
        Assert.Equal(480, ugc.TicksPerBeat);
        Assert.Equal("BASIC", ugc.Difficulty);
        Assert.NotEmpty(ugc.Notes);
        Assert.NotEmpty(ugc.BpmEvents);
    }

    [Fact]
    public void ConvertedUgcHasCorrectNoteCount()
    {
        var c2s = LoadC2s();
        var xml = LoadMusicXml();
        var ugc = new C2sToUgcConverter().Convert(c2s, xml);

        int tap = ugc.Notes.Where(n => n.Type == NoteType.TAP).Count();
        int chr = ugc.Notes.Where(n => n.Type == NoteType.CHR).Count();
        int hld = ugc.Notes.Where(n => n.Type == NoteType.HLD).Count();
        int sld = ugc.Notes.Where(n => n.Type is NoteType.SLD or NoteType.SLC).Count();
        int air = ugc.Notes.Where(n => n.Type == NoteType.AIR).Count();
        int ahd = ugc.Notes.Where(n => n.Type == NoteType.AHD).Count();

        Assert.Equal(192, tap);
        Assert.Equal(11, chr);
        Assert.Equal(8, hld);
        Assert.True(sld >= 6);
        Assert.Equal(2, air);
        Assert.Equal(6, ahd);
    }

    [Fact]
    public void UgcSerializationProducesValidOutput()
    {
        var c2s = LoadC2s();
        var xml = LoadMusicXml();
        var ugc = new C2sToUgcConverter().Convert(c2s, xml);
        var text = UgcSerializer.Serialize(ugc);

        Assert.Contains("@VER", text);
        Assert.Contains("@TICKS\t480", text);
        Assert.Contains("@BPM\t0'0", text);
        Assert.Contains("@BEAT\t0\t", text);
        Assert.Contains("@ENDHEAD", text);
        Assert.Contains("@TITLE\tB.B.K.K.B.K.K.", text);
        Assert.Contains("@ARTIST\tnora2r", text);
    }

    [Fact]
    public void CanParseSerializedUgcBack()
    {
        var c2s = LoadC2s();
        var xml = LoadMusicXml();
        var ugc = new C2sToUgcConverter().Convert(c2s, xml);
        var text = UgcSerializer.Serialize(ugc);

        var reparsed = UgcParser.Parse(text);

        Assert.NotNull(reparsed);
        Assert.NotEmpty(reparsed.Notes);
        Assert.NotEmpty(reparsed.BpmEvents);
        Assert.Equal("B.B.K.K.B.K.K.", reparsed.Title);
    }

    [Fact]
    public void TikcScalingIsReversible()
    {
        var c2s = LoadC2s();
        var xml = LoadMusicXml();
        var ugc = new C2sToUgcConverter().Convert(c2s, xml);

        var noteAt96 = c2s.Notes.FirstOrDefault(n =>
            n.Type == NoteType.TAP && n.Measure == 5 && n.Offset == 96);
        Assert.NotNull(noteAt96);

        var ugcNote = ugc.Notes.FirstOrDefault(n =>
            n.Type == NoteType.TAP && n.Measure == 5 && n.Offset == 480);
        Assert.NotNull(ugcNote);
    }

    [Fact]
    public void HoldDurationScalesCorrectly()
    {
        var c2s = LoadC2s();
        var xml = LoadMusicXml();
        var ugc = new C2sToUgcConverter().Convert(c2s, xml);

        var hld = ugc.Notes.FirstOrDefault(n => n.Type == NoteType.HLD && n.Measure == 12);
        Assert.NotNull(hld);
        Assert.Equal(960, hld.HoldDuration);
    }

    [Fact]
    public void AirTargetIsResolvedToN()
    {
        var c2s = LoadC2s();
        var xml = LoadMusicXml();
        var ugc = new C2sToUgcConverter().Convert(c2s, xml);

        var air = ugc.Notes.FirstOrDefault(n => n.Type == NoteType.AIR && n.Measure == 12);
        Assert.NotNull(air);
        Assert.Equal("N", air.TargetNote);
    }

    [Fact]
    public void CanDumpUgcOutput()
    {
        var c2s = LoadC2s();
        var xml = LoadMusicXml();
        var ugc = new C2sToUgcConverter().Convert(c2s, xml);
        var text = UgcSerializer.Serialize(ugc);

        var outPath = Path.Combine(ExampleDir, "0003_c2s_to_ugc.ugc");
        File.WriteAllText(outPath, text);

        Assert.True(File.Exists(outPath));
        Assert.Contains("@TITLE\t", text);
        Assert.Contains("@ARTIST\t", text);
        Assert.Contains("@LEVEL\t4", text);
        Assert.Contains("@CONST\t4.00000", text);
    }

    [Fact]
    public void MusicXmlProvidesCorrectMetadata()
    {
        var c2s = LoadC2s();
        var xml = LoadMusicXml();
        var ugc = new C2sToUgcConverter().Convert(c2s, xml);

        Assert.Equal("B.B.K.K.B.K.K.", ugc.Title);
        Assert.Equal("nora2r", ugc.Artist);
        Assert.Equal(4, ugc.Level);
        Assert.Equal(4.0, ugc.Constant, 0.01);
        Assert.Equal("BASIC", ugc.Difficulty);
        Assert.Equal("ロシェ＠ペンギン", ugc.Designer);
    }
}
