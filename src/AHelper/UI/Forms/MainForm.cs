using AHelper.Models;
using AHelper.Services;

namespace AHelper.UI.Forms;

/// <summary>
/// Hosts the compact tray-popup experience while hardware behavior remains in dedicated services.
/// </summary>
public sealed class MainForm : Form
{
    private const string ApplicationTitle = "A-Helper";
    private const int PopupWidth = 540;
    private const int PopupHeight = 350;
    private const int ScreenEdgeMargin = 10;
    private const int CardHeight = 60;

    private static readonly PerformanceMode[] PlaceholderModes = Enum.GetValues<PerformanceMode>();

    private readonly Dictionary<PerformanceMode, Button> _modeButtons = [];
    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly Label _statusLabel;
    private readonly NotifyIcon _trayIcon;

    private bool _isExiting;

    /// <summary>
    /// Creates the popup shell and restores preferences without performing hardware writes.
    /// </summary>
    public MainForm()
    {
        _settingsService = new SettingsService();
        _settings = _settingsService.Load();

        ConfigureWindow();

        var rootLayout = CreateRootLayout();
        Controls.Add(rootLayout);

        rootLayout.Controls.Add(CreateHeader());
        rootLayout.Controls.Add(CreateModeSection());
        rootLayout.Controls.Add(CreateTelemetrySection());

        _statusLabel = CreateStatusLabel();
        rootLayout.Controls.Add(_statusLabel);

        UpdateModeSelection(_settings.SelectedMode);

        _trayIcon = CreateTrayIcon();

        Load += (_, _) => PositionPopup(Screen.PrimaryScreen ?? Screen.FromControl(this));
        Resize += OnWindowResize;
        FormClosing += OnWindowClosing;
    }

    private void ConfigureWindow()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = UiPalette.WindowBackground;
        ClientSize = new Size(PopupWidth, PopupHeight);
        Font = new Font("Segoe UI", 9F);
        ForeColor = UiPalette.PrimaryText;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Text = ApplicationTitle;
    }

    private static TableLayoutPanel CreateRootLayout()
    {
        var layout = new TableLayoutPanel
        {
            BackColor = UiPalette.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 12),
            RowCount = 4,
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        return layout;
    }

    private static Control CreateHeader()
    {
        var header = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
        };

        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));

        header.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = UiPalette.PrimaryText,
            Text = ApplicationTitle,
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, 0);

        header.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = UiPalette.SecondaryText,
            Text = "Alienware x16 R1",
            TextAlign = ContentAlignment.MiddleRight,
        }, 1, 0);

        return header;
    }

    private Control CreateModeSection()
    {
        var section = CreateSection("Performance mode");
        var cardLayout = new TableLayoutPanel
        {
            ColumnCount = PlaceholderModes.Length,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 6, 0, 0),
            RowCount = 1,
        };

        for (var index = 0; index < PlaceholderModes.Length; index++)
        {
            cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / PlaceholderModes.Length));

            var mode = PlaceholderModes[index];
            var button = CreateModeButton(mode);
            _modeButtons.Add(mode, button);
            cardLayout.Controls.Add(button, index, 0);
        }

        section.Controls.Add(cardLayout, 0, 1);
        return section;
    }

    private static TableLayoutPanel CreateSection(string title)
    {
        var section = new TableLayoutPanel
        {
            BackColor = UiPalette.SectionBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(10, 8, 10, 8),
            RowCount = 2,
        };

        section.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        section.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        section.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = UiPalette.PrimaryText,
            Text = title,
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, 0);

        return section;
    }

    private Button CreateModeButton(PerformanceMode mode)
    {
        var button = new Button
        {
            BackColor = UiPalette.CardBackground,
            Cursor = Cursors.Hand,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            ForeColor = UiPalette.PrimaryText,
            Margin = new Padding(3),
            MinimumSize = new Size(0, CardHeight),
            Tag = mode,
            Text = mode.ToString(),
            UseVisualStyleBackColor = false,
        };

        button.FlatAppearance.BorderColor = UiPalette.CardBorder;
        button.FlatAppearance.BorderSize = 1;
        button.Click += OnModeButtonClick;

        return button;
    }

    private static Control CreateTelemetrySection()
    {
        var section = CreateSection("Hardware status");
        var telemetry = new TableLayoutPanel
        {
            ColumnCount = 3,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 6, 0, 0),
            RowCount = 1,
        };

        telemetry.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        telemetry.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        telemetry.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334F));
        telemetry.Controls.Add(CreateTelemetryLabel("CPU", "-- °C"), 0, 0);
        telemetry.Controls.Add(CreateTelemetryLabel("GPU", "-- °C"), 1, 0);
        telemetry.Controls.Add(CreateTelemetryLabel("Fans", "Discovery pending"), 2, 0);

        section.Controls.Add(telemetry, 0, 1);
        return section;
    }

    private static Control CreateTelemetryLabel(string name, string value)
    {
        var panel = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(3),
            RowCount = 2,
        };

        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = UiPalette.MutedText,
            Text = name,
            TextAlign = ContentAlignment.BottomCenter,
        }, 0, 0);
        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = UiPalette.SecondaryText,
            Text = value,
            TextAlign = ContentAlignment.TopCenter,
        }, 0, 1);

        return panel;
    }

    private static Label CreateStatusLabel()
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = UiPalette.MutedText,
            Text = "Hardware discovery is not connected yet.",
            TextAlign = ContentAlignment.MiddleLeft,
        };
    }

    private NotifyIcon CreateTrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open A-Helper", null, (_, _) => ShowWindow());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());

        var trayIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = SystemIcons.Application,
            Text = ApplicationTitle,
            Visible = true,
        };

        trayIcon.MouseClick += (_, eventArgs) =>
        {
            if (eventArgs.Button == MouseButtons.Left)
            {
                ToggleWindow();
            }
        };

        return trayIcon;
    }

    private void OnModeButtonClick(object? sender, EventArgs eventArgs)
    {
        if (sender is not Button { Tag: PerformanceMode mode })
        {
            return;
        }

        _settings.SelectedMode = mode;
        _settingsService.Save(_settings);
        UpdateModeSelection(mode);
        _statusLabel.Text = $"{mode} selected in the UI; no hardware command was sent.";
    }

    private void UpdateModeSelection(PerformanceMode selectedMode)
    {
        foreach (var (mode, button) in _modeButtons)
        {
            var selected = mode == selectedMode;
            button.BackColor = selected ? UiPalette.AccentBackground : UiPalette.CardBackground;
            button.FlatAppearance.BorderColor = selected ? UiPalette.AccentBorder : UiPalette.CardBorder;
        }
    }

    private void OnWindowResize(object? sender, EventArgs eventArgs)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
            WindowState = FormWindowState.Normal;
            return;
        }

        if (Visible)
        {
            PositionPopup(Screen.FromControl(this));
        }
    }

    private void OnWindowClosing(object? sender, FormClosingEventArgs eventArgs)
    {
        if (_isExiting)
        {
            return;
        }

        eventArgs.Cancel = true;
        Hide();
    }

    private void ToggleWindow()
    {
        if (Visible)
        {
            Hide();
            return;
        }

        ShowWindow();
    }

    private void ShowWindow()
    {
        WindowState = FormWindowState.Normal;
        PositionPopup(Screen.FromPoint(Cursor.Position));
        Show();
        Activate();
        BringToFront();
    }

    private void PositionPopup(Screen screen)
    {
        var workArea = screen.WorkingArea;
        Location = new Point(
            workArea.Right - Width - ScreenEdgeMargin,
            workArea.Bottom - Height - ScreenEdgeMargin);
    }

    private void ExitApplication()
    {
        _isExiting = true;
        _settingsService.Save(_settings);
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }
}
