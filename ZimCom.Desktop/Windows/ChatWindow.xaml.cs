using System.Windows;
using System.Windows.Forms;
using ZimCom.Core.Models;
using ZimCom.Desktop.ViewModels;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace ZimCom.Desktop.Windows;

/// <summary>
/// Companion Window for all kinds of message
/// </summary>
public partial class ChatWindow : Window
{
    public ChatWindowViewModel ViewModel { get; }
    /// <inheritdoc />
    public ChatWindow(User user)
    {
        InitializeComponent();
        ViewModel = new ChatWindowViewModel(user);
        DataContext = ViewModel;
    }
}