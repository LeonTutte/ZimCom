using System.Windows;

namespace ZimCom.Desktop.Windows;

/// <summary>
///     Interaktionslogik für MessageWindows.xaml
/// </summary>
public partial class MessageWindow
{
    /// <summary>
    /// Provides a modal dialog that displays a short title and message to the user.
    /// The window is configured as a non‑resizable tool window centered on the screen
    /// with a fixed height and width suitable for brief notifications or error messages.
    /// </summary>
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