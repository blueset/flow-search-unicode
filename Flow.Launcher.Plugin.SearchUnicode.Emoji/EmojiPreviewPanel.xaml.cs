using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.SearchUnicode.Emoji
{
    public partial class EmojiPreviewPanel : UserControl
    {
        public EmojiPreviewPanel(EmojiInfo emojiInfo, string pluginDirectory)
        {
            EmojiInfo = emojiInfo ?? throw new ArgumentNullException(nameof(emojiInfo));
            PluginDirectory = pluginDirectory ?? throw new ArgumentNullException(nameof(pluginDirectory));
            InitializeComponent();
        }

        private EmojiInfo EmojiInfo { get; }

        private string PluginDirectory { get; }

        public ImageSource EmojiImage
        {
            get
            {
                try
                {
                    var path = Path.Combine(PluginDirectory, EmojiInfo.ImageRelativePath);
                    if (!File.Exists(path))
                    {
                        return null;
                    }

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                catch
                {
                    return null;
                }
            }
        }

        public string DisplayName => EmojiInfo?.Name ?? string.Empty;

        public string Keywords => EmojiInfo?.CldrFull ?? string.Empty;

        public string Codepoint => EmojiInfo?.Codepoint ?? string.Empty;

        public string Group => EmojiInfo?.Group ?? string.Empty;

        public string Subgroup => EmojiInfo?.Subgroup ?? string.Empty;
    }
}
