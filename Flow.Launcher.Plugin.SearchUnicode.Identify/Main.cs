using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Controls;
using Flow.Launcher.Plugin;

using Flow.Launcher.Plugin.SearchUnicode.Utils;

namespace Flow.Launcher.Plugin.SearchUnicode.Identify
{
    public class IdentifyPlugin : IPlugin, IContextMenu, ISettingProvider
    {

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
                            Title = "Identify Unicode",
                            SubTitle = "Type any text to identify Unicode characters",
                            ActionKeywordAssigned = "uid",
                            Glyph = new GlyphInfo("Segoe Fluent Icons", "\ue721"), // Search
                        }
                    }
                );
            }

            var (stdout, stderr) = ExecuteUni("identify", new List<string> { query.Search });
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

            var result = new List<Result>();

            if (chars.Count > 0)
            {
                result.Add(new Result
                {
                    Title = "Hex sequence in Unicode",
                    SubTitle = string.Join(" ", chars.Select(c => c.Hex)),
                    ActionKeywordAssigned = "uid",
                    Glyph = new GlyphInfo("sans-serif", "0x"),
                });
                result.Add(new Result
                {
                    Title = "Decimal sequence in Unicode",
                    SubTitle = string.Join(" ", chars.Select(c => c.Decimal)),
                    ActionKeywordAssigned = "uid",
                    Glyph = new GlyphInfo("sans-serif", "123"),
                });
                result.Add(new Result
                {
                    Title = "UTF-8 sequence",
                    SubTitle = string.Join(" ", chars.Select(c => c.Utf8)),
                    ActionKeywordAssigned = "uid",
                    Glyph = new GlyphInfo("sans-serif", "U8"),
                });
                result.Add(new Result
                {
                    Title = "XML sequence",
                    SubTitle = string.Join(" ", chars.Select(c => c.Xml)),
                    ActionKeywordAssigned = "uid",
                    Glyph = new GlyphInfo("sans-serif", "&"),
                });
            }


            result.AddRange(chars.Select(c => new Result
            {
                Title = $"{c.Char} — {c.Name}",
                SubTitle = $"{c.Codepoint} ({c.Decimal}) {c.Block} ({c.Category})",
                ActionKeywordAssigned = "uid",
                CopyText = Char.ConvertFromUtf32(int.Parse(c.Decimal)).ToString(),
                Glyph = new GlyphInfo("sans-serif", c.Char),
                ContextData = c,
                PreviewPanel = new Lazy<UserControl>(() => new UnicodePreviewPanel(c)),
				Action = _ =>
                {
                    var settings = _context.API.LoadSettingJsonStorage<Settings>();
                    System.Windows.Clipboard.SetText(Char.ConvertFromUtf32(int.Parse(c.Decimal)).ToString());

                    if (settings.SelectedAction == "Copy and paste")
                    {
                        // fire-and-forget the paste emulation so Action returns immediately
                        WaitWindowHideAndSimulatePaste();
                    }

                    return true;
                }
            }).ToList());

            return result;
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            if (selectedResult.ContextData is not CharInfo charInfo)
            {
                return new List<Result>();
            }
            if (charInfo == null)
            {
                return new List<Result>();
            }

            return charInfo.GetContextMenu("uid");
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