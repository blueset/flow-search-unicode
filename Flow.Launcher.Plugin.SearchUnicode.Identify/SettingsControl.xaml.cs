using System.Windows;
using System.Windows.Controls;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.SearchUnicode.Identify
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        private Settings _settings;
        private readonly PluginInitContext _context;

        public SettingsControl(PluginInitContext context)
        {
            _context = context;
            _settings = _context.API.LoadSettingJsonStorage<Settings>();

            InitializeComponent();

            // Find the ComboBoxItem that matches the saved setting
            foreach (ComboBoxItem item in ActionComboBox.Items)
            {
                if (item.Content.ToString() == _settings.SelectedAction)
                {
                    item.IsSelected = true;
                    break;
                }
            }

            // If no item was selected, default to first option
            if (ActionComboBox.SelectedItem == null)
            {
                ActionComboBox.SelectedIndex = 0;
                if (ActionComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    _settings.SelectedAction = selectedItem.Content.ToString();
                    _context.API.SaveSettingJsonStorage<Settings>();
                }
            }

            ActionComboBox.SelectionChanged += ActionComboBox_SelectionChanged;
        }

        private void ActionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActionComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _settings.SelectedAction = selectedItem.Content.ToString();
                _context.API.SaveSettingJsonStorage<Settings>();
            }
        }
    }
}
