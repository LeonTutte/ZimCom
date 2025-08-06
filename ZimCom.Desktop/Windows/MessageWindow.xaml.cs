using System.Windows;

namespace ZimCom.Desktop.Windows;

/// <summary>
///     Interaktionslogik für MessageWindows.xaml
/// </summary>
public partial class MessageWindow
{
    public MessageWindow(string title, string message)
    {
        InitializeComponent();
        Title = title;
        AddressBox.Text = message;
    }

    private void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}