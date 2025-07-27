using System.Windows;

using ZimCom.Desktop.ViewModels;

namespace ZimCom.Desktop.Windows {
    /// <summary>
    /// Interaktionslogik für SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window {
        private SettingsViewModel _viewModel;
        public SettingsWindow() {
            InitializeComponent();
            _viewModel = new SettingsViewModel();
            this.DataContext = _viewModel;
        }
    }
}
