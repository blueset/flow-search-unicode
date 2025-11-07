using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using Flow.Launcher.Plugin.SearchUnicode.Utils;

namespace Flow.Launcher.Plugin.SearchUnicode.Search
{
    public partial class SearchPlugin : IPlugin, IContextMenu, ISettingProvider
    {
        // Pattern components for readability
        private const string PATTERN_SINGLE_HEX = @"((U\+|0x|%x)?[0-9A-Fa-f]+)";
        private const string PATTERN_SINGLE_BIN = @"0b[01]+";
        private const string PATTERN_SINGLE_DEC = @"0d[0-9]+";
        private const string PATTERN_SINGLE_OCT = @"0o[0-7]+";
        private const string PATTERN_SINGLE_VALUE = @"(" + PATTERN_SINGLE_HEX + @"|" + PATTERN_SINGLE_BIN + @"|" + PATTERN_SINGLE_DEC + @"|" + PATTERN_SINGLE_OCT + @")";
        
        // Combined pattern matching any of: single hex, binary, decimal, octal, range, or utf8 patterns
        private const string PRINT_PATTERN = @"^(" + PATTERN_SINGLE_HEX + @"|" + PATTERN_SINGLE_BIN + @"|" + PATTERN_SINGLE_DEC + @"|" + PATTERN_SINGLE_OCT + @"|" + PATTERN_SINGLE_VALUE + @"(\.\.|-)" + PATTERN_SINGLE_VALUE + @"|utf8:" + PATTERN_SINGLE_HEX + @"|utf8:[0-9A-Fa-f]+)$";
        
        [GeneratedRegex(PRINT_PATTERN, RegexOptions.IgnoreCase)]
        private static partial Regex UnicodeHexRegex();

        private PluginInitContext _context;

        public void Init(PluginInitContext context)
        {
            _context = context;
        }

        private (string stdout, string stderr) ExecuteUni(string action, IEnumerable<string> query) {
            
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "uni.exe"),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };
            startInfo.ArgumentList.Add("-as");
            startInfo.ArgumentList.Add("json");
            startInfo.ArgumentList.Add("-format");
            startInfo.ArgumentList.Add("all");
            startInfo.ArgumentList.Add(action);
            startInfo.ArgumentList.Add("--");
            query.ToList().ForEach(q => startInfo.ArgumentList.Add(q));

            using (var process = System.Diagnostics.Process.Start(startInfo))
            {
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                return (stdout, stderr);
            }
        }

        public List<Result> Query(Query query)
        {
            if (string.IsNullOrWhiteSpace(query.Search))
            {
                return new List<Result>(
                    new Result[] {
                        new Result {
                            Title = "Search Unicode",
                            SubTitle = "Type a keyword to search for a Unicode character",
                            ActionKeywordAssigned = "u",
                            Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue721"), // Search
                        }
                    }
                );
            }

            var args = SharedUtilities.SplitArgs(query.Search);

            var (stdout, stderr) = ExecuteUni("search", args);
            var chars = new List<CharInfo>();

            if (stdout.Length > 0) {
                try
                {
                    chars.AddRange(JsonSerializer.Deserialize<List<CharInfo>>(stdout));
                }
                catch (JsonException e)
                {
                    throw new Exception($"Failed to parse JSON. StdOut = [{stdout}], StdErr = [{stderr}]", e);
                }
            }

            if (args.All(arg => UnicodeHexRegex().IsMatch(arg))) {

                var (pstdout, _) = ExecuteUni("print", args);
                if (pstdout.Length > 0) {
                    try {
                        chars.AddRange(JsonSerializer.Deserialize<List<CharInfo>>(pstdout));
                    } catch (JsonException e) {
                        throw new Exception($"Failed to parse JSON. StdOut = [{stdout}], StdErr = [{stderr}]", e);
                    }
                }
            }
            
            if (chars.Count == 0) {
                return new List<Result> {
                    new Result {
                        Title = "No matches found",
                        SubTitle = "Try searching for something else",
                        ActionKeywordAssigned = "u",
                        Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue721"), // Search
                    }
                };
            }

            return chars.Take(20).Select(c => new Result
            {
                Title = $"{c.Char} — {c.Name}",
                SubTitle = $"{c.Codepoint} ({c.Decimal}) {c.Block} ({c.Category})",
                ActionKeywordAssigned = "u",
                CopyText = Char.ConvertFromUtf32(int.Parse(c.Decimal)).ToString(),
                Glyph = new GlyphInfo("sans-serif", c.Char),
                ContextData = c,
                Action = _ =>
                {
                    var settings = _context.API.LoadSettingJsonStorage<Settings>();
                    var text = Char.ConvertFromUtf32(int.Parse(c.Decimal)).ToString();
                    System.Windows.Clipboard.SetText(text);

                    if (settings.SelectedAction == "Copy and paste")
                    {
                        WaitWindowHideAndSimulatePaste();
                    }
                    return true;
                }
            }).ToList();
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            if (selectedResult.ContextData is not CharInfo) {
                return new List<Result>();
            }
            var charInfo = selectedResult.ContextData as CharInfo;
            if (charInfo == null)
            {
                return new List<Result>();
            }

            return new List<Result>
            {
                new Result
                {
                    Title = "Binary representation",
                    SubTitle = charInfo.Binary,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("sans-serif", "01"),
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Binary);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Unicode block",
                    SubTitle = charInfo.Block,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\uF158"), // dial shape 3 (cube)
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Block);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Category",
                    SubTitle = charInfo.Category,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue7bc"), // Reading list
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Category);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Character",
                    SubTitle = charInfo.Char,
                    CopyText = Char.ConvertFromUtf32(int.Parse(charInfo.Decimal)).ToString(),
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("sans-serif", charInfo.Char),
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(Char.ConvertFromUtf32(int.Parse(charInfo.Decimal)).ToString());
                        return true;
                    }
                },
                new Result
                {
                    Title = "Codepoint",
                    SubTitle = charInfo.Codepoint,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("sans-serif", "U+"),
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Codepoint);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Decimal representation",
                    SubTitle = charInfo.Decimal,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("sans-serif", "123"),
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Decimal);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Digraph",
                    SubTitle = charInfo.Digraph,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("sans-serif", "AB"),
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Digraph);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Hexadecimal representation",
                    SubTitle = charInfo.Hex,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("sans-serif", "0x"),
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Hex);
                        return true;
                    }
                },
                new Result
                {
                    Title = "HTML entity",
                    SubTitle = charInfo.Html,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("sans-serif", "&"),
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Html);
                        return true;
                    }
                },
                new Result
                {
                    Title = "JSON representation",
                    SubTitle = charInfo.Json,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue943"), // Code ({})
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Json);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Key symbol",
                    SubTitle = charInfo.KeySymbol,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue92e"), // Keyboard Standard
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.KeySymbol);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Name",
                    SubTitle = charInfo.Name,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue8ac"), // Rename
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Name);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Octal representation",
                    SubTitle = charInfo.Oct,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("sans-serif", "012"),
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Oct);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Plane",
                    SubTitle = charInfo.Plane,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue81e"), // Map layers
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Plane);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Properties",
                    SubTitle = charInfo.Props,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue8fd"), // Bulleted list
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Props);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Script",
                    SubTitle = charInfo.Script,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\uf2b7"), // Locale Language
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Script);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Unicode version introduced",
                    SubTitle = charInfo.Unicode,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue81c"), // History
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Unicode);
                        return true;
                    }
                },
                new Result
                {
                    Title = "UTF-16 (big-endian)",
                    SubTitle = charInfo.Utf16BE,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("sans-serif", "BE"),
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Utf16BE);
                        return true;
                    }
                },
                new Result
                {
                    Title = "UTF-16 (little-endian)",
                    SubTitle = charInfo.Utf16LE,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("sans-serif", "LE"),
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Utf16LE);
                        return true;
                    }
                },
                new Result
                {
                    Title = "UTF-8",
                    SubTitle = charInfo.Utf8,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("sans-serif", "U8"),
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Utf8);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Width",
                    SubTitle = charInfo.Width,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue145"), // dock left
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Width);
                        return true;
                    }
                },
                new Result
                {
                    Title = "XML entity",
                    SubTitle = charInfo.Xml,
                    ActionKeywordAssigned = "u",
                    Glyph = new GlyphInfo("sans-serif", "&"),
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(charInfo.Xml);
                        return true;
                    }
                }
            }.Where(r => !string.IsNullOrWhiteSpace(r.SubTitle)).ToList();
        }

        public Control CreateSettingPanel()
        {
            return new SettingsControl(_context);
        }

        private async Task WaitWindowHideAndSimulatePaste()
        {
            while (_context!.API.IsMainWindowVisible())
            {
                await Task.Delay(100);
            }
            // new InputSimulator().Keyboard.ModifiedKeyStroke(
            //     VirtualKeyCode.CONTROL,
            //     VirtualKeyCode.VK_V
            // );
            System.Windows.Forms.SendKeys.SendWait("^v");
        }
    }

    public class Settings
    {
        private string _selectedAction = "Copy to clipboard";

        public string SelectedAction
        {
            get
            {
                return _selectedAction;
            }
            set
            {
                _selectedAction = value;
            }
        }
    }

    public class CharInfo
    {
        [JsonPropertyName("bin")]
        public string Binary { get; set; }
        [JsonPropertyName("block")]
        public string Block { get; set; }
        [JsonPropertyName("cat")]
        public string Category { get; set; }
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
    }
}
