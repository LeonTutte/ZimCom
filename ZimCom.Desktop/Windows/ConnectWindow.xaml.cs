using System.Windows;

using ZimCom.Desktop.ViewModels;

namespace ZimCom.Desktop.Windows {
    /// <summary>
    /// Interaktionslogik für ConnectWindow.xaml
    /// </summary>
    public partial class ConnectWindow : Window {
        private ConnectWindowViewModel _viewModel;
        public ConnectWindowViewModel ViewModel { get { return _viewModel; } }
        public ConnectWindow() {
            InitializeComponent();
            _viewModel = new ConnectWindowViewModel();
            this.DataContext = _viewModel;
        }
    }
}
