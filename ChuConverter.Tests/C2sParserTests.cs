using System;
using System.IO;
using System.Linq;
using ChuConverter;
using ChuConverter.Models;
using Xunit;

namespace ChuConverter.Tests;

public class C2sParserTests
{
    private static string ExampleDir => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "examples");
    private static string OfficialC2s => Path.Combine(ExampleDir, "0003_00.c2s");

    [Fact]
    public void CanParseOfficialBaseC2s()
    {
        if (!File.Exists(OfficialC2s))
            throw new SkipException($"Official C2S not found: {OfficialC2s}");

        var chart = C2sParser.Parse(File.ReadAllText(OfficialC2s));

        Assert.NotNull(chart);
        Assert.Equal(3, chart.MusicId);
        Assert.Equal(0, chart.DifficultId);
        Assert.Equal(384, chart.Resolution);
        Assert.Contains("ロシェ", chart.Creator);
        Assert.NotEmpty(chart.BpmEvents);
        Assert.NotEmpty(chart.Notes);
    }

    [Fact]
    public void OfficialC2sBpmEvents()
    {
        if (!File.Exists(OfficialC2s))
            throw new SkipException($"Official C2S not found: {OfficialC2s}");

        var chart = C2sParser.Parse(File.ReadAllText(OfficialC2s));

        Assert.Single(chart.BpmEvents);
        var bpm = chart.BpmEvents[0];
        Assert.Equal(0, bpm.Measure);
        Assert.Equal(0, bpm.Offset);
        Assert.Equal(170.0, bpm.Bpm, 0.001);
    }

    [Fact]
    public void OfficialC2sFirstNoteCorrect()
    {
        if (!File.Exists(OfficialC2s))
            throw new SkipException($"Official C2S not found: {OfficialC2s}");

        var chart = C2sParser.Parse(File.ReadAllText(OfficialC2s));
        var notes = chart.Notes.OrderBy(n => n.TotalTick(384)).ToList();

        var first = notes[0];
        Assert.Equal(NoteType.TAP, first.Type);
        Assert.Equal(5, first.Measure);
        Assert.Equal(0, first.Offset);
        Assert.Equal(8, first.Cell);
        Assert.Equal(4, first.Width);
    }

    [Fact]
    public void OfficialC2sHasHoldAndAir()
    {
        if (!File.Exists(OfficialC2s))
            throw new SkipException($"Official C2S not found: {OfficialC2s}");

        var chart = C2sParser.Parse(File.ReadAllText(OfficialC2s));

        var hld = chart.Notes.FirstOrDefault(n => n.Type == NoteType.HLD && n.Measure == 12);
        Assert.NotNull(hld);
        Assert.Equal(192, hld.HoldDuration);

        var air = chart.Notes.FirstOrDefault(n => n.Type == NoteType.AIR && n.Measure == 12);
        Assert.NotNull(air);
        Assert.Equal("HLD", air.TargetNote);
    }

    [Fact]
    public void OfficialC2sHasSlideNotes()
    {
        if (!File.Exists(OfficialC2s))
            throw new SkipException($"Official C2S not found: {OfficialC2s}");

        var chart = C2sParser.Parse(File.ReadAllText(OfficialC2s));

        var sld = chart.Notes.FirstOrDefault(n => n.Type == NoteType.SLD);
        Assert.NotNull(sld);
        Assert.True(sld.SlideDuration > 0);
        Assert.True(sld.EndCell >= 0);
    }

    [Fact]
    public void OfficialC2sNoteCount()
    {
        if (!File.Exists(OfficialC2s))
            throw new SkipException($"Official C2S not found: {OfficialC2s}");

        var chart = C2sParser.Parse(File.ReadAllText(OfficialC2s));

        int tap = chart.Notes.Where(n => n.Type == NoteType.TAP).Count();
        int chr = chart.Notes.Where(n => n.Type == NoteType.CHR).Count();
        int hld = chart.Notes.Where(n => n.Type == NoteType.HLD).Count();
        int sld = chart.Notes.Where(n => n.Type is NoteType.SLD or NoteType.SLC).Count();
        int air = chart.Notes.Where(n => n.Type == NoteType.AIR).Count();
        int ahd = chart.Notes.Where(n => n.Type == NoteType.AHD).Count();

        Assert.Equal(192, tap);
        Assert.Equal(11, chr);
        Assert.Equal(8, hld);
        Assert.Equal(8, sld);
        Assert.Equal(2, air);
        Assert.Equal(6, ahd);
    }

    [Fact]
    public void RoundTripPreservesNotes()
    {
        if (!File.Exists(OfficialC2s))
            throw new SkipException($"Official C2S not found: {OfficialC2s}");

        var original = C2sParser.Parse(File.ReadAllText(OfficialC2s));
        var serialized = C2sSerializer.Serialize(original);
        var reparsed = C2sParser.Parse(serialized);

        Assert.Equal(original.Notes.Count, reparsed.Notes.Count);

        var origNotes = original.Notes.OrderBy(n => n.TotalTick(384)).ToList();
        var reNotes = reparsed.Notes.OrderBy(n => n.TotalTick(384)).ToList();

        for (int i = 0; i < origNotes.Count; i++)
        {
            Assert.Equal(origNotes[i].Type, reNotes[i].Type);
            Assert.Equal(origNotes[i].Measure, reNotes[i].Measure);
            Assert.Equal(origNotes[i].Offset, reNotes[i].Offset);
            Assert.Equal(origNotes[i].Cell, reNotes[i].Cell);
            Assert.Equal(origNotes[i].Width, reNotes[i].Width);
        }
    }
}
