using System.Xml.Linq;

namespace ChuConverter.Models;

public class MusicXmlData
{
    public string DataName { get; set; } = "";
    public string Title { get; set; } = "";
    public string SortName { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Genre { get; set; } = "";
    public string CueFileName { get; set; } = "";
    public string JacketFile { get; set; } = "";
    public string ReleaseVersion { get; set; } = "";

    public List<FumenInfo> Fumens { get; } = new();

    public FumenInfo? GetFumen(int difficultId)
        => Fumens.FirstOrDefault(f => f.Id == difficultId);
}

public class FumenInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Data { get; set; } = "";
    public bool Enable { get; set; }
    public string FilePath { get; set; } = "";
    public int Level { get; set; }
    public int LevelDecimal { get; set; }
    public double Constant => Level + LevelDecimal / 100.0;
    public string NotesDesigner { get; set; } = "";
}

public static class MusicXmlParser
{
    public static MusicXmlData Parse(string xmlPath)
    {
        var data = new MusicXmlData();
        var doc = XDocument.Load(xmlPath);
        var root = doc.Element("MusicData");
        if (root == null) return data;

        data.DataName = ElementValue(root, "dataName");
        data.Title = NestedStr(root, "name");
        data.SortName = ElementValue(root, "sortName");
        data.Artist = NestedStr(root, "artistName");
        data.CueFileName = NestedStr(root, "cueFileName");
        data.JacketFile = NestedPath(root, "jaketFile");
        data.ReleaseVersion = NestedStr(root, "releaseTagName");

        var genreNames = root.Element("genreNames");
        if (genreNames != null)
        {
            var firstGenre = genreNames.Descendants("StringID").FirstOrDefault();
            if (firstGenre != null)
                data.Genre = NestedStr(firstGenre, "str");
        }

        var fumens = root.Element("fumens");
        if (fumens != null)
        {
            foreach (var fumenEl in fumens.Elements("MusicFumenData"))
            {
                var fumen = new FumenInfo
                {
                    Id = IntElement(fumenEl, "type", "id"),
                    Name = NestedStr(fumenEl, "type", "str"),
                    Data = NestedStr(fumenEl, "type", "data"),
                    Enable = ElementValue(fumenEl, "enable") == "true",
                    FilePath = NestedPath(fumenEl, "file"),
                    Level = IntElement(fumenEl, "level"),
                    LevelDecimal = IntElement(fumenEl, "levelDecimal"),
                    NotesDesigner = ElementValue(fumenEl, "notesDesigner"),
                };
                data.Fumens.Add(fumen);
            }
        }

        return data;
    }

    private static string ElementValue(XElement parent, string name)
        => parent.Element(name)?.Value ?? "";

    private static int IntElement(XElement parent, string name)
        => int.TryParse(parent.Element(name)?.Value, out var v) ? v : 0;

    private static int IntElement(XElement parent, string parent2, string name)
        => int.TryParse(parent.Element(parent2)?.Element(name)?.Value, out var v) ? v : 0;

    private static string NestedStr(XElement parent, string name)
        => parent.Element(name)?.Element("str")?.Value ?? "";

    private static string NestedStr(XElement parent, string name1, string name2)
        => parent.Element(name1)?.Element(name2)?.Value ?? "";

    private static string NestedPath(XElement parent, string name)
        => parent.Element(name)?.Element("path")?.Value ?? "";
}
