using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using Timer = System.Windows.Forms.Timer;

#nullable enable

namespace PKHeX.TemplateRegen.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        Text = "PKHeX Template Regenerator";
        Size = new Size(900, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = LoadAppIcon();

        _mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10)
        };
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        _infoPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle
        };

        var infoLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(10)
        };
        infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var titleLabel = new Label
        {
            Text = "Repository Status",
            Font = new System.Drawing.Font("Segoe UI", 12, FontStyle.Bold),
            Dock = DockStyle.Fill
        };
        infoLayout.Controls.Add(titleLabel, 0, 0);
        infoLayout.SetColumnSpan(titleLabel, 2);

        infoLayout.Controls.Add(new Label { Text = "PKHeX Path:", Dock = DockStyle.Fill }, 0, 1);
        _pkHexPathLabel = new Label { Dock = DockStyle.Fill, AutoEllipsis = true };
        infoLayout.Controls.Add(_pkHexPathLabel, 1, 1);

        infoLayout.Controls.Add(new Label { Text = "Events Gallery:", Dock = DockStyle.Fill }, 0, 2);
        _evGalPathLabel = new Label { Dock = DockStyle.Fill, AutoEllipsis = true };
        infoLayout.Controls.Add(_evGalPathLabel, 1, 2);

        infoLayout.Controls.Add(new Label { Text = "PoGo Enc Tool:", Dock = DockStyle.Fill }, 0, 3);
        _pgetPathLabel = new Label { Dock = DockStyle.Fill, AutoEllipsis = true };
        infoLayout.Controls.Add(_pgetPathLabel, 1, 3);

        _infoPanel.Controls.Add(infoLayout);

        var controlPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(5)
        };

        _updateButton = new Button
        {
            Text = "Update Now",
            Size = new Size(120, 40),
            Margin = new Padding(5)
        };

        _settingsButton = new Button
        {
            Text = "⚙ Settings",
            Size = new Size(100, 40),
            Margin = new Padding(5)
        };

        _autoDetectButton = new Button
        {
            Text = "🔍 Auto Detect",
            Size = new Size(120, 40),
            Margin = new Padding(5)
        };

        _autoUpdateCheckBox = new CheckBox
        {
            Text = "Auto Update Every",
            AutoSize = true,
            Margin = new Padding(20, 12, 5, 5)
        };

        _autoUpdateInterval = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 24,
            Value = 6,
            Width = 50,
            Margin = new Padding(0, 10, 5, 5)
        };

        var hoursLabel = new Label
        {
            Text = "hours",
            AutoSize = true,
            Margin = new Padding(0, 12, 20, 5)
        };

        _lastUpdateLabel = new Label
        {
            Text = "Never updated",
            AutoSize = true,
            Margin = new Padding(20, 12, 5, 5)
        };

        controlPanel.Controls.AddRange(new Control[]
        {
            _updateButton, _settingsButton, _autoDetectButton,
            _autoUpdateCheckBox, _autoUpdateInterval, hoursLabel, _lastUpdateLabel
        });

        var logPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle
        };

        _logTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new System.Drawing.Font("Consolas", 9),
            WordWrap = false
        };

        logPanel.Controls.Add(_logTextBox);

        var statusPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle
        };

        _statusLabel = new Label
        {
            Text = "Ready",
            Dock = DockStyle.Left,
            AutoSize = true,
            Padding = new Padding(5, 5, 0, 0)
        };

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Right,
            Width = 200,
            Style = ProgressBarStyle.Continuous
        };

        statusPanel.Controls.Add(_statusLabel);
        statusPanel.Controls.Add(_progressBar);

        _mainLayout.Controls.Add(_infoPanel, 0, 0);
        _mainLayout.Controls.Add(controlPanel, 0, 1);
        _mainLayout.Controls.Add(logPanel, 0, 2);
        _mainLayout.Controls.Add(statusPanel, 0, 3);

        Controls.Add(_mainLayout);

        _updateWorker = new BackgroundWorker
        {
            WorkerReportsProgress = true,
            WorkerSupportsCancellation = true
        };
        _statusTimer = new Timer(components) { Interval = 1000 };
    }

    #endregion

    private BackgroundWorker _updateWorker = null!;
    private Timer _statusTimer = null!;
    private NotifyIcon? _trayIcon;
    private RichTextBox _logTextBox = null!;
    private ProgressBar _progressBar = null!;
    private Label _statusLabel = null!;
    private Button _updateButton = null!;
    private Button _settingsButton = null!;
    private Button _autoDetectButton = null!;
    private Panel _infoPanel = null!;
    private Label _pkHexPathLabel = null!;
    private Label _evGalPathLabel = null!;
    private Label _pgetPathLabel = null!;
    private CheckBox _autoUpdateCheckBox = null!;
    private NumericUpDown _autoUpdateInterval = null!;
    private Label _lastUpdateLabel = null!;
    private TableLayoutPanel _mainLayout = null!;
}
