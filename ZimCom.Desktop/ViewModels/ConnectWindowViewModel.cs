using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ZimCom.Desktop.ViewModels {
    public partial class ConnectWindowViewModel : ObservableObject {
        public EventHandler<string>? ConnectToAddress { get; set; }
        public EventHandler? CloseWindow { get; set; }
        [ObservableProperty]
        public partial string Address { get; set; }
        [RelayCommand]
        public void Connect() {
            ConnectToAddress?.Invoke(this, Address);
            CloseWindow?.Invoke(this, EventArgs.Empty);
        }
    }
}
