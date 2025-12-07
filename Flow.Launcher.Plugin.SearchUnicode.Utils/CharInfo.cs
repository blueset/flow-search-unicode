using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.SearchUnicode.Utils
{
    public class CharInfo
    {
        [JsonPropertyName("aliases")]
        public string Aliases { get; set; }
        [JsonPropertyName("bin")]
        public string Binary { get; set; }
        [JsonPropertyName("block")]
        public string Block { get; set; }
        [JsonPropertyName("cat")]
        public string Category { get; set; }
        [JsonPropertyName("cells")]
        public string Cells { get; set; }
        [JsonPropertyName("char")]
        public string Char { get; set; }
        [JsonPropertyName("cpoint")]
        public string Codepoint { get; set; }
        [JsonPropertyName("dec")]
        public string Decimal { get; set; }
        [JsonPropertyName("digraph")]
        public string Digraph { get; set; }
        [JsonPropertyName("hex")]
        public string Hex { get; set; }
        [JsonPropertyName("html")]
        public string Html { get; set; }
        [JsonPropertyName("json")]
        public string Json { get; set; }
        [JsonPropertyName("keysym")]
        public string KeySymbol { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("oct")]
        public string Oct { get; set; }
        [JsonPropertyName("plane")]
        public string Plane { get; set; }
        [JsonPropertyName("props")]
        public string Props { get; set; }
        [JsonPropertyName("refs")]
        public string Refs { get; set; }
        [JsonPropertyName("script")]
        public string Script { get; set; }
        [JsonPropertyName("unicode")]
        public string Unicode { get; set; }
        [JsonPropertyName("utf16be")]
        public string Utf16BE { get; set; }
        [JsonPropertyName("utf16le")]
        public string Utf16LE { get; set; }
        [JsonPropertyName("utf8")]
        public string Utf8 { get; set; }
        [JsonPropertyName("width")]
        public string Width { get; set; }
        [JsonPropertyName("xml")]
        public string Xml { get; set; }

        public List<Result> GetContextMenu(string actionKeyword)
        {
            return new List<Result>
            {
                new Result
                {
                    Title = "Aliases",
                    SubTitle = Aliases,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue8ec"), // Tag
                    CopyText = Aliases,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Aliases);
                        return true;
                    }    
                },
                new Result
                {
                    Title = "Similars and Alternatives",
                    SubTitle = Refs,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\uec6c"), // Favicon2
                    CopyText = Refs,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Refs);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Binary representation",
                    SubTitle = Binary,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("sans-serif", "01"),
                    CopyText = Binary,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Binary);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Unicode block",
                    SubTitle = Block,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\uF158"), // dial shape 3 (cube)
                    CopyText = Block,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Block);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Category",
                    SubTitle = Category,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue7bc"), // Reading list
                    CopyText = Category,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Category);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Character",
                    SubTitle = Char,
                    ActionKeywordAssigned = actionKeyword,
                    CopyText = System.Char.ConvertFromUtf32(int.Parse(Decimal)).ToString(),
                    Glyph = new GlyphInfo("sans-serif", Char),
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(System.Char.ConvertFromUtf32(int.Parse(Decimal)).ToString());
                        return true;
                    }
                },
                new Result
                {
                    Title = "Codepoint",
                    SubTitle = Codepoint,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("sans-serif", "U+"),
                    CopyText = Codepoint,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Codepoint);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Decimal representation",
                    SubTitle = Decimal,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("sans-serif", "123"),
                    CopyText = Decimal,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Decimal);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Number of Cells to Display as",
                    SubTitle = Cells,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue746"), // ResizeMouseTall
                    CopyText = Cells,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Cells);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Digraph",
                    SubTitle = Digraph,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("sans-serif", "AB"),
                    CopyText = Digraph,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Digraph);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Hexadecimal representation",
                    SubTitle = Hex,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("sans-serif", "0x"),
                    CopyText = Hex,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Hex);
                        return true;
                    }
                },
                new Result
                {
                    Title = "HTML entity",
                    SubTitle = Html,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("sans-serif", "&"),
                    CopyText = Html,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Html);
                        return true;
                    }
                },
                new Result
                {
                    Title = "JSON representation",
                    SubTitle = Json,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue943"), // Code ({})
                    CopyText = Json,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Json);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Key symbol",
                    SubTitle = KeySymbol,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue92e"), // Keyboard Standard
                    CopyText = KeySymbol,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(KeySymbol);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Name",
                    SubTitle = Name,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue8ac"), // Rename
                    CopyText = Name,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Name);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Octal representation",
                    SubTitle = Oct,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("sans-serif", "012"),
                    CopyText = Oct,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Oct);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Plane",
                    SubTitle = Plane,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue81e"), // Map layers
                    CopyText = Plane,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Plane);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Properties",
                    SubTitle = Props,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue8fd"), // Bulleted list
                    CopyText = Props,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Props);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Script",
                    SubTitle = Script,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\uf2b7"), // Locale Language
                    CopyText = Script,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Script);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Unicode version introduced",
                    SubTitle = Unicode,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue81c"), // History
                    CopyText = Unicode,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Unicode);
                        return true;
                    }
                },
                new Result
                {
                    Title = "UTF-16 (big-endian)",
                    SubTitle = Utf16BE,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("sans-serif", "BE"),
                    CopyText = Utf16BE,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Utf16BE);
                        return true;
                    }
                },
                new Result
                {
                    Title = "UTF-16 (little-endian)",
                    SubTitle = Utf16LE,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("sans-serif", "LE"),
                    CopyText = Utf16LE,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Utf16LE);
                        return true;
                    }
                },
                new Result
                {
                    Title = "UTF-8",
                    SubTitle = Utf8,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("sans-serif", "U8"),
                    CopyText = Utf8,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Utf8);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Width",
                    SubTitle = Width,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue145"), // dock left
                    CopyText = Width,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Width);
                        return true;
                    }
                },
                new Result
                {
                    Title = "XML entity",
                    SubTitle = Xml,
                    ActionKeywordAssigned = actionKeyword,
                    Glyph = new GlyphInfo("sans-serif", "&"),
                    CopyText = Xml,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Xml);
                        return true;
                    }
                }
            }.Where(r => !string.IsNullOrWhiteSpace(r.SubTitle)).ToList();
        }
    }
}
