using System;
using System.Windows;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.SearchUnicode.Utils
{
    public partial class UnicodePreviewPanel : UserControl
    {
        public UnicodePreviewPanel(CharInfo charInfo)
        {
            CharInfo = charInfo ?? throw new ArgumentNullException(nameof(charInfo));
            InitializeComponent();
        }

        private CharInfo CharInfo { get; }

        public string Glyph => CharInfo?.Char ?? string.Empty;

        public string DisplayName => string.IsNullOrWhiteSpace(CharInfo?.Name)
            ? Glyph
            : CharInfo!.Name;

        public string Aliases => CharInfo?.Aliases ?? string.Empty;

        public string Codepoint => CharInfo?.Codepoint ?? string.Empty;

        public string Category => CharInfo?.Category ?? string.Empty;

        public string Block => CharInfo?.Block ?? string.Empty;

        public Visibility AliasVisibility => string.IsNullOrWhiteSpace(Aliases)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }
}
