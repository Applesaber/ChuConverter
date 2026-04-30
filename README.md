# ChuConverter

CHUNITHM 谱面格式转换工具 — C2S / UGC / SUS 三向互转。

## 项目结构

```
Converter/
├── ChuConverter.sln              # 解决方案
├── ChuConverter/                 # CLI工具
│   └── Program.cs
├── ChuConverter.Model/           # 转换引擎（可引用为库）
│   ├── Models/
│   │   ├── C2sChart.cs           #   C2S 模型
│   │   ├── UgcChart.cs           #   UGC 模型
│   │   ├── SusChart.cs           #   SUS 模型
│   │   └── MusicXmlData.cs       #   Music.xml 解析
│   ├── C2sParser.cs / C2sSerializer.cs
│   ├── UgcParser.cs / UgcSerializer.cs
│   ├── SusParser.cs / SusSerializer.cs
│   ├── UgcToC2sConverter.cs / C2sToUgcConverter.cs
│   ├── SusToC2sConverter.cs / C2sToSusConverter.cs
│   └── MusicXmlParser.cs
└── ChuConverter.debug/           # 测试 (xUnit, 26 项)
```

## 构建

```bash
dotnet build ChuConverter.sln
```

## 命令行使用

```bash
# 帮助
dotnet run --project ChuConverter

# UGC ↔ C2S
dotnet run --project ChuConverter -- ugc2c2s <input.ugc> [output.c2s]
dotnet run --project ChuConverter -- c2s2ugc <input.c2s> <Music.xml> [output.ugc]

# SUS ↔ C2S
dotnet run --project ChuConverter -- c2s2sus <input.c2s> [title] [artist] [output.sus]
dotnet run --project ChuConverter -- sus2c2s <input.sus> [output.c2s]
```

### 示例

```bash
dotnet run --project ChuConverter -- ugc2c2s bas.ugc
dotnet run --project ChuConverter -- c2s2ugc 0003_00.c2s Music.xml out.ugc
dotnet run --project ChuConverter -- c2s2sus 0003_00.c2s "B.B.K.K.B.K.K." nora2r
```

## 作为库使用

```csharp
using ChuConverter;
using ChuConverter.Models;

// ── UGC → C2S ──
var ugc = UgcParser.Parse(File.ReadAllText("bas.ugc"));
var c2s = new UgcToC2sConverter().Convert(ugc);
File.WriteAllText("out.c2s", C2sSerializer.Serialize(c2s));

// ── C2S → UGC (需要 Music.xml) ──
var c2s = C2sParser.Parse(File.ReadAllText("0003_00.c2s"));
var xml = MusicXmlParser.Parse("Music.xml");
var ugc = new C2sToUgcConverter().Convert(c2s, xml);
File.WriteAllText("out.ugc", UgcSerializer.Serialize(ugc));

// ── C2S → SUS ──
var sus = new C2sToSusConverter().Convert(c2s, "曲名", "曲师");
File.WriteAllText("out.sus", SusSerializer.Serialize(sus));

// ── SUS → C2S ──
var sus2 = SusParser.Parse(File.ReadAllText("input.sus"));
var c2s2 = new SusToC2sConverter().Convert(sus2);
```

## 格式说明

| 格式 | 分辨率 | tick 含义 | 说明 |
|------|--------|-----------|------|
| C2S | 384 | 每小节 | 官方游戏引擎格式，纯谱面数据 |
| UGC | 480 | 每拍 | UMIGURI 编辑器格式，含完整元数据 |
| SUS | 480 | 每拍 | 社区工具格式 (SUSPlayer/Seaurchin) |

### 换算关系

```
C2S tick = UGC tick × 96 / 480   (×1/5)
UGC tick = C2S tick × 480 / 96   (×5)
SUS lane = C2S cell × 2
SUS width = C2S width × 2
```


## 测试

```bash
dotnet test                              # 全量 26 项
dotnet test --filter "UgcParser"         # 分类测试
dotnet test --filter "C2sToUgc"          # C2S→UGC 测试
```

## 依赖

- .NET 9 SDK
- `Microsoft.Extensions.Logging.Abstractions`
- 测试: xUnit

## 参考

- [Suprnova/Chunithm-Research](https://github.com/Suprnova/Chunithm-Research) — C2S 格式文档
