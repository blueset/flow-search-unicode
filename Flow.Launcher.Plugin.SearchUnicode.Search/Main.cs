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

        private (string stdout, string stderr) ExecuteUni(string action, IEnumerable<string> query)
        {

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
                if (string.IsNullOrWhiteSpace(query.ActionKeyword))
                {
                    return new List<Result>();
                }

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

            if (stdout.Length > 0)
            {
                try
                {
                    chars.AddRange(JsonSerializer.Deserialize<List<CharInfo>>(stdout));
                }
                catch (JsonException e)
                {
                    throw new Exception($"Failed to parse JSON. StdOut = [{stdout}], StdErr = [{stderr}]", e);
                }
            }

            if (args.All(arg => UnicodeHexRegex().IsMatch(arg)))
            {
                var (pstdout, _) = ExecuteUni("print", args);
                if (pstdout.Length > 0)
                {
                    try
                    {
                        chars.AddRange(JsonSerializer.Deserialize<List<CharInfo>>(pstdout));
                    }
                    catch (JsonException e)
                    {
                        throw new Exception($"Failed to parse JSON. StdOut = [{stdout}], StdErr = [{stderr}]", e);
                    }
                }
            }

            if (chars.Count == 0)
            {
                if (string.IsNullOrWhiteSpace(query.ActionKeyword))
                {
                    return new List<Result>();
                }

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
                TitleHighlightData = SharedUtilities.GetMatchingCharacterIndices(_context.API, $"{c.Char} — {c.Name}", args),
				ContextData = c,
				PreviewPanel = new Lazy<UserControl>(() => new UnicodePreviewPanel(c)),
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
            if (selectedResult.ContextData is not CharInfo)
            {
                return new List<Result>();
            }
            var charInfo = selectedResult.ContextData as CharInfo;
            if (charInfo == null)
            {
                return new List<Result>();
            }

            return charInfo.GetContextMenu("u");
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
}
