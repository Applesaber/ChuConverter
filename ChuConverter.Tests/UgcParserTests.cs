using System;
using System.IO;
using ChuConverter;
using Xunit;

namespace ChuConverter.Tests;

public class UgcParserTests
{
    private static string ExampleDir => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "examples");

    [Fact]
    public void CanParseBasicUgcFile()
    {
        string filePath = Path.Combine(ExampleDir, "0003_bas.ugc");
        if (!File.Exists(filePath))
            throw new SkipException($"Test file not found: {filePath}");

        string content = File.ReadAllText(filePath);
        var chart = UgcParser.Parse(content);

        Assert.NotNull(chart);
        Assert.Equal("BASIC", chart.Difficulty);
        Assert.Equal(480, chart.TicksPerBeat);
        Assert.NotEmpty(chart.Notes);
        Assert.NotEmpty(chart.BpmEvents);
    }
}

public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
}
