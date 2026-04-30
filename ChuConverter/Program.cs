using ChuConverter;
using ChuConverter.Models;
using Microsoft.Extensions.Logging;

if (args.Length < 2)
{
    Console.WriteLine("ChuConverter — CHUNITHM 谱面格式转换工具");
    Console.WriteLine();
    Console.WriteLine("用法:");
    Console.WriteLine("  ChuConverter ugc2c2s  <input.ugc> [output.c2s]");
    Console.WriteLine("  ChuConverter c2s2ugc  <input.c2s> [output.ugc]");
    Console.WriteLine("  ChuConverter c2s2sus  <input.c2s> [output.sus]");
    Console.WriteLine("  ChuConverter sus2c2s  <input.sus> [output.c2s]");
    Console.WriteLine();
    Console.WriteLine("示例:");
    Console.WriteLine("  ChuConverter ugc2c2s bas.ugc");
    Console.WriteLine("  ChuConverter c2s2ugc 0003_00.c2s");
    Console.WriteLine("  ChuConverter c2s2sus 0003_00.c2s");
    return;
}

var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
var command = args[0].ToLowerInvariant();

try
{
    switch (command)
    {
        case "ugc2c2s":
            ConvertUgcToC2s(args, loggerFactory);
            break;
        case "c2s2ugc":
            ConvertC2sToUgc(args, loggerFactory);
            break;
        case "c2s2sus":
            ConvertC2sToSus(args);
            break;
        case "sus2c2s":
            ConvertSusToC2s(args);
            break;
        default:
            Console.Error.WriteLine($"未知命令: {command}");
            Console.Error.WriteLine("支持: ugc2c2s, c2s2ugc");
            break;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"错误: {ex.Message}");
}

static void ConvertUgcToC2s(string[] args, ILoggerFactory loggerFactory)
{
    if (args.Length < 2) { Console.Error.WriteLine("用法: ugc2c2s <input.ugc> [output.c2s]"); return; }

    string inputPath = args[1];
    string outputPath = args.Length >= 3 ? args[2] : Path.ChangeExtension(inputPath, ".c2s");

    if (!File.Exists(inputPath)) { Console.Error.WriteLine($"文件不存在: {inputPath}"); return; }

    var parser = new UgcParser(loggerFactory.CreateLogger<UgcParser>());
    var converter = new UgcToC2sConverter(loggerFactory.CreateLogger<UgcToC2sConverter>());

    Console.WriteLine($"解析 UGC: {inputPath}");
    var ugc = parser.ParseUgc(File.ReadAllText(inputPath));

    Console.WriteLine($"转换中... ({ugc.Notes.Count} 个音符)");
    var c2s = converter.Convert(ugc);

    Console.WriteLine($"写入 C2S: {outputPath}");
    File.WriteAllText(outputPath, C2sSerializer.Serialize(c2s));

    Console.WriteLine("完成!");
}

static void ConvertC2sToUgc(string[] args, ILoggerFactory loggerFactory)
{
    if (args.Length < 2) { Console.Error.WriteLine("用法: c2s2ugc <input.c2s> [output.ugc]"); return; }

    string inputPath = args[1];
    string outputPath = args.Length >= 3 ? args[2] : Path.ChangeExtension(inputPath, ".ugc");
    string xmlPath = Path.Combine(Path.GetDirectoryName(inputPath)!, "Music.xml");

    if (!File.Exists(inputPath)) { Console.Error.WriteLine($"文件不存在: {inputPath}"); return; }
    if (!File.Exists(xmlPath)) { Console.Error.WriteLine($"Music.xml 不存在: {xmlPath} (C2S→UGC 需要 Music.xml 提供元数据)"); return; }

    var parser = new C2sParser(loggerFactory.CreateLogger<C2sParser>());
    var converter = new C2sToUgcConverter(loggerFactory.CreateLogger<C2sToUgcConverter>());

    Console.WriteLine($"解析 C2S: {inputPath}");
    var c2s = parser.ParseChart(File.ReadAllText(inputPath));

    Console.WriteLine($"解析 Music.xml: {xmlPath}");
    var musicXml = MusicXmlParser.Parse(xmlPath);

    Console.WriteLine($"转换中... ({c2s.Notes.Count} 个音符, 曲名: {musicXml.Title})");
    var ugc = converter.Convert(c2s, musicXml);

    Console.WriteLine($"写入 UGC: {outputPath}");
    File.WriteAllText(outputPath, UgcSerializer.Serialize(ugc));

    Console.WriteLine("完成!");
}

static void ConvertC2sToSus(string[] args)
{
    if (args.Length < 2) { Console.Error.WriteLine("用法: c2s2sus <input.c2s> [output.sus]"); return; }

    string inputPath = args[1];
    string outputPath = args.Length >= 3 ? args[2] : Path.ChangeExtension(inputPath, ".sus");
    string dir = Path.GetDirectoryName(inputPath)!;
    string xmlPath = Path.Combine(dir, "Music.xml");

    if (!File.Exists(inputPath)) { Console.Error.WriteLine($"文件不存在: {inputPath}"); return; }

    var c2s = C2sParser.Parse(File.ReadAllText(inputPath));

    string title = "";
    string artist = "";
    if (File.Exists(xmlPath))
    {
        var xml = MusicXmlParser.Parse(xmlPath);
        title = xml.Title;
        artist = xml.Artist;
        Console.WriteLine($"Music.xml: {title} / {artist}");
    }

    var sus = new C2sToSusConverter().Convert(c2s, title, artist);

    Console.WriteLine($"写入 SUS: {outputPath} ({sus.Notes.Count} 个音符)");
    File.WriteAllText(outputPath, SusSerializer.Serialize(sus));
    Console.WriteLine("完成!");
}

static void ConvertSusToC2s(string[] args)
{
    if (args.Length < 2) { Console.Error.WriteLine("用法: sus2c2s <input.sus> [output.c2s]"); return; }

    string inputPath = args[1];
    string outputPath = args.Length >= 3 ? args[2] : Path.ChangeExtension(inputPath, ".c2s");

    if (!File.Exists(inputPath)) { Console.Error.WriteLine($"文件不存在: {inputPath}"); return; }

    var sus = SusParser.Parse(File.ReadAllText(inputPath));
    var c2s = new SusToC2sConverter().Convert(sus);

    Console.WriteLine($"写入 C2S: {outputPath} ({c2s.Notes.Count} 个音符)");
    File.WriteAllText(outputPath, C2sSerializer.Serialize(c2s));
    Console.WriteLine("完成!");
}
