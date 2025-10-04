using System.Windows;
using ZimCom.Core.Models;
using ZimCom.Desktop.ViewModels;

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