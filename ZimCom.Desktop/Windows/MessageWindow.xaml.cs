using System.Windows;

namespace ZimCom.Desktop.Windows;
/// <summary>
/// Interaktionslogik für MessageWindows.xaml
/// </summary>
public partial class MessageWindow : Window {
    public MessageWindow(string title, string message) {
        InitializeComponent();
        this.Title = title;
        this.AddressBox.Text = message;
    }

    private void ConnectButton_Click(object sender, RoutedEventArgs e) {
        this.Close();
    }
}
