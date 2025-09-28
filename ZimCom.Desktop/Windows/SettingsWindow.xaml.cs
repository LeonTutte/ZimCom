using ZimCom.Desktop.ViewModels;

namespace ZimCom.Desktop.Windows;

/// <summary>
///     Interaktionslogik für SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow
{
    /// <summary>
    /// Provides access to the view model that supplies data and behavior for
    /// the <see cref="SettingsWindow"/>.
    /// </summary>
    /// <remarks>
    /// The <c>ViewModel</c> property is instantiated with a new instance of
    /// <see cref="SettingsViewModel"/> when the window is created. It is used as
    /// the DataContext for data binding between XAML controls and the view model’s
    /// observable properties.
    /// </remarks>
    public SettingsViewModel ViewModel { get; } = new();

    /// <summary>
    /// Handles the user interface and interaction logic for the settings dialog.
    /// </summary>
    /// <remarks>
    /// The window is initialized with a <see cref="SettingsViewModel"/> instance that
    /// holds all data bindings for the UI. When instantiated, the constructor
    /// calls <c>InitializeComponent</c> to load XAML resources and sets the
    /// <see cref="DataContext"/> to the view‑model so that controls can bind to its
    /// properties.
    /// </remarks>
    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }
}