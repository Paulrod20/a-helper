using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AHelper.ViewModels;
using Wpf.Ui.Controls;

namespace AHelper;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    /// <summary>Gap kept between the window edges and the work area, matching G-Helper's tray-popup anchoring.</summary>
    private const double ScreenEdgeMargin = 10;

    // WinForms is used here only for NotifyIcon - WPF has no native tray icon control.
    private readonly System.Windows.Forms.NotifyIcon _trayIcon;

    // Set right before a real shutdown so Closing knows not to just hide the window instead.
    private bool _isExiting;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        SizeChanged += OnSizeChanged;
        StateChanged += OnStateChanged;
        Closing += OnClosing;

        _trayIcon = CreateTrayIcon();
    }

    /// <summary>
    /// Builds the system tray icon: left-click toggles the window, right-click offers Exit
    /// since hiding-instead-of-closing (see OnClosing) removes the window's own way to quit.
    /// </summary>
    private System.Windows.Forms.NotifyIcon CreateTrayIcon()
    {
        var trayIcon = new System.Windows.Forms.NotifyIcon
        {
            // TODO: swap for a real A-Helper .ico once one exists.
            Icon = System.Drawing.SystemIcons.Application,
            Text = "A-Helper",
            Visible = true,
            ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(),
        };

        trayIcon.ContextMenuStrip.Items.Add("Exit", null, (_, _) => ExitApplication());
        trayIcon.MouseClick += OnTrayIconMouseClick;

        return trayIcon;
    }

    private void OnTrayIconMouseClick(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (e.Button != System.Windows.Forms.MouseButtons.Left)
            return;

        if (IsVisible)
        {
            Hide();
        }
        else
        {
            Show();
            Activate();
        }
    }

    /// <summary>
    /// Re-anchors the window to the bottom-right of the work area every time its size changes,
    /// since SizeToContent means height shifts as sections are added/removed.
    /// </summary>
    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ScreenEdgeMargin - Width;
        Top = workArea.Bottom - ScreenEdgeMargin - Height;
    }

    /// <summary>Minimizing drops straight to the tray instead of showing on the taskbar.</summary>
    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            WindowState = WindowState.Normal;
        }
    }

    /// <summary>Clicking the window's own close button hides it to the tray rather than exiting.</summary>
    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isExiting)
            return;

        e.Cancel = true;
        Hide();
    }

    private void ExitApplication()
    {
        _isExiting = true;
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        System.Windows.Application.Current.Shutdown();
    }
}