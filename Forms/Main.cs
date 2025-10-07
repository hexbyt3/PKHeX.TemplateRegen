using System.ComponentModel;
using PKHeX.TemplateRegen.Managers;
using PKHeX.TemplateRegen.Core;

namespace PKHeX.TemplateRegen.Forms;

public partial class MainForm : Form
{
    private readonly ProgramSettings _settings;
    private DateTime _lastUpdateTime = DateTime.MinValue;

    public MainForm()
    {
        _settings = SettingsManager.LoadSettings();

        InitializeComponent();
        SetupDarkTheme();
        SetupEventHandlers();
        SetupTrayIcon();
        UpdatePathLabels();

        AppLogManager.Initialize(_logTextBox);
    }

    private void SetupDarkTheme()
    {
        var darkBg = Color.FromArgb(30, 30, 30);
        var darkControl = Color.FromArgb(45, 45, 48);
        var darkBorder = Color.FromArgb(62, 62, 66);
        var lightText = Color.FromArgb(241, 241, 241);
        var accentColor = Color.FromArgb(0, 122, 204);

        BackColor = darkBg;
        ForeColor = lightText;

        void ApplyThemeToControl(Control control)
        {
            if (control is Button btn)
            {
                btn.BackColor = darkControl;
                btn.ForeColor = lightText;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = darkBorder;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 62, 66);
                btn.FlatAppearance.MouseDownBackColor = accentColor;
            }
            else if (control is TextBox || control is RichTextBox || control is NumericUpDown)
            {
                control.BackColor = darkControl;
                control.ForeColor = lightText;
            }
            else if (control is Panel || control is GroupBox || control is TableLayoutPanel || control is FlowLayoutPanel)
            {
                control.BackColor = darkBg;
                control.ForeColor = lightText;
            }
            else if (control is Label || control is CheckBox)
            {
                control.ForeColor = lightText;
            }
            else if (control is ProgressBar pb)
            {
                pb.BackColor = darkControl;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child);
            }
        }

        ApplyThemeToControl(this);
    }

    private void SetupEventHandlers()
    {
        _updateButton.Click += async (s, e) => await PerformUpdate();
        _settingsButton.Click += (s, e) => ShowSettingsDialog();
        _autoDetectButton.Click += async (s, e) => await AutoDetectRepositories();
        _autoUpdateCheckBox.CheckedChanged += OnAutoUpdateToggled;
        _statusTimer.Tick += OnStatusTimerTick;
        _updateWorker.DoWork += OnUpdateWorkerDoWork;
        _updateWorker.ProgressChanged += OnUpdateWorkerProgressChanged;
        _updateWorker.RunWorkerCompleted += OnUpdateWorkerCompleted;
        FormClosing += OnFormClosing;

        _statusTimer.Start();
    }

    private void SetupTrayIcon()
    {
        try
        {
            _trayIcon = new NotifyIcon
            {
                Icon = LoadAppIcon() ?? SystemIcons.Application,
                Text = "PKHeX Template Regenerator",
                Visible = true,
                ContextMenuStrip = CreateTrayMenu()
            };

            _trayIcon.DoubleClick += (s, e) =>
            {
                try
                {
                    Show();
                    WindowState = FormWindowState.Normal;
                    BringToFront();
                }
                catch (Exception ex)
                {
                    AppLogManager.LogError("Error showing form from tray", ex);
                }
            };
        }
        catch (Exception ex)
        {
            AppLogManager.LogError("Error setting up tray icon", ex);
            _trayIcon = null;
        }
    }

    private ContextMenuStrip CreateTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Update Now", null, async (s, e) => await PerformUpdate());
        menu.Items.Add("Settings", null, (s, e) => ShowSettingsDialog());
        menu.Items.Add("-");
        menu.Items.Add("Exit", null, (s, e) => Application.Exit());

        ApplyDarkThemeToMenu(menu);
        return menu;
    }

    private void ApplyDarkThemeToMenu(ContextMenuStrip menu)
    {
        menu.BackColor = Color.FromArgb(45, 45, 48);
        menu.ForeColor = Color.FromArgb(241, 241, 241);
        menu.Renderer = new DarkMenuRenderer();
    }

    private async Task PerformUpdate()
    {
        if (_updateWorker.IsBusy)
        {
            MessageBox.Show("Update already in progress!", "Information",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _updateButton.Enabled = false;
        _progressBar.Value = 0;
        _progressBar.Style = ProgressBarStyle.Marquee;
        _statusLabel.Text = "Starting update...";

        await Task.Run(() => _updateWorker.RunWorkerAsync());
    }

    private void OnUpdateWorkerDoWork(object? sender, DoWorkEventArgs e)
    {
        try
        {
            _updateWorker.ReportProgress(10, "Validating paths...");
            if (!ValidatePaths())
            {
                e.Result = new UpdateResult { Success = false, Message = "Invalid paths configuration" };
                return;
            }

            _updateWorker.ReportProgress(20, "Creating backups...");
            BackupManager.CreateBackup(_settings.PathPKHeX);

            _updateWorker.ReportProgress(30, "Updating repositories...");

            _updateWorker.ReportProgress(40, "Processing Events Gallery...");
            var mgdb = new MGDBPickler(_settings.PathPKHeX, _settings.PathRepoEvGal);
            mgdb.Update();

            _updateWorker.ReportProgress(70, "Processing PoGo Enc Tool...");
            var pget = new PGETPickler(_settings.PathPKHeX, _settings.PathRepoPGET, _settings.AutoManagePGETRepo);
            pget.Update();

            _updateWorker.ReportProgress(100, "Update completed!");
            e.Result = new UpdateResult { Success = true, Message = "All templates regenerated successfully!" };
        }
        catch (Exception ex)
        {
            e.Result = new UpdateResult { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    private void OnUpdateWorkerProgressChanged(object? sender, ProgressChangedEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => OnUpdateWorkerProgressChanged(sender, e)));
            return;
        }

        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Value = e.ProgressPercentage;
        _statusLabel.Text = e.UserState?.ToString() ?? "Processing...";
    }

    private void OnUpdateWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => OnUpdateWorkerCompleted(sender, e)));
            return;
        }

        _updateButton.Enabled = true;
        _progressBar.Style = ProgressBarStyle.Continuous;

        if (e.Result is UpdateResult result)
        {
            _statusLabel.Text = result.Message;

            if (result.Success)
            {
                _lastUpdateTime = DateTime.Now;
                UpdateLastUpdateLabel();

                ShowBalloonTip(3000, "Update Complete",
                    "Templates regenerated successfully!", ToolTipIcon.Info);
            }
            else
            {
                MessageBox.Show(result.Message, "Update Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async Task AutoDetectRepositories()
    {
        _autoDetectButton.Enabled = false;
        _statusLabel.Text = "Searching for repositories...";

        var detector = new RepositoryDetector();
        var results = await detector.DetectRepositoriesAsync();

        if (results.Any())
        {
            using var dialog = new AutoDetectDialog(results);
            if (dialog.ShowDialog() == DialogResult.OK && dialog.SelectedPaths != null)
            {
                var (pkHex, evGal, pget) = dialog.SelectedPaths.Value;

                if (!string.IsNullOrEmpty(pkHex))
                    _settings.RepoPKHeXLegality = pkHex;
                if (!string.IsNullOrEmpty(evGal))
                    _settings.RepoNameEvGal = evGal;
                if (!string.IsNullOrEmpty(pget))
                    _settings.RepoNamePGET = pget;

                SettingsManager.SaveSettings(_settings);
                UpdatePathLabels();
                AppLogManager.Log("Repository paths updated from auto-detection");
            }
        }
        else
        {
            MessageBox.Show("No repositories found. Please clone them first or specify paths manually.",
                "Auto-Detection", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        _autoDetectButton.Enabled = true;
        _statusLabel.Text = "Ready";
    }

    private void ShowSettingsDialog()
    {
        using var dialog = new SettingsDialog(_settings);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            SettingsManager.SaveSettings(_settings);
            UpdatePathLabels();
            AppLogManager.Log("Settings updated");
        }
    }

    private void UpdatePathLabels()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(UpdatePathLabels));
            return;
        }

        _pkHexPathLabel.Text = Directory.Exists(_settings.PathPKHeX)
            ? $"✓ {_settings.PathPKHeX}"
            : $"✗ {_settings.PathPKHeX} (Not Found)";

        _evGalPathLabel.Text = Directory.Exists(_settings.PathRepoEvGal)
            ? $"✓ {_settings.PathRepoEvGal}"
            : $"✗ {_settings.PathRepoEvGal} (Not Found)";

        // PoGoEncTool path display depends on auto-management setting
        if (_settings.AutoManagePGETRepo)
        {
            if (Directory.Exists(_settings.PathRepoPGET))
            {
                _pgetPathLabel.Text = $"✓ {_settings.PathRepoPGET} (Auto-managed)";
                _pgetPathLabel.ForeColor = Color.LightGreen;
            }
            else
            {
                _pgetPathLabel.Text = $"⚙ {_settings.PathRepoPGET} (Will auto-clone on first update)";
                _pgetPathLabel.ForeColor = Color.LightBlue;
            }
        }
        else
        {
            _pgetPathLabel.Text = Directory.Exists(_settings.PathRepoPGET)
                ? $"✓ {_settings.PathRepoPGET}"
                : $"✗ {_settings.PathRepoPGET} (Not Found)";
            _pgetPathLabel.ForeColor = Directory.Exists(_settings.PathRepoPGET)
                ? Color.LightGreen : Color.LightCoral;
        }

        _pkHexPathLabel.ForeColor = Directory.Exists(_settings.PathPKHeX)
            ? Color.LightGreen : Color.LightCoral;
        _evGalPathLabel.ForeColor = Directory.Exists(_settings.PathRepoEvGal)
            ? Color.LightGreen : Color.LightCoral;
    }

    private void UpdateLastUpdateLabel()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(UpdateLastUpdateLabel));
            return;
        }

        if (_lastUpdateTime != DateTime.MinValue)
        {
            var elapsed = DateTime.Now - _lastUpdateTime;
            _lastUpdateLabel.Text = elapsed.TotalMinutes < 1
                ? "Just updated"
                : $"Last update: {_lastUpdateTime:g}";
        }
    }

    private bool ValidatePaths()
    {
        // PKHeX and EventsGallery paths must exist
        if (!Directory.Exists(_settings.PathPKHeX) || !Directory.Exists(_settings.PathRepoEvGal))
            return false;

        // PoGoEncTool path only needs to exist if auto-management is disabled
        // (when enabled, we'll clone it automatically)
        if (!_settings.AutoManagePGETRepo && !Directory.Exists(_settings.PathRepoPGET))
            return false;

        return true;
    }

    private void OnAutoUpdateToggled(object? sender, EventArgs e)
    {
        if (_autoUpdateCheckBox.Checked)
        {
            ScheduleNextUpdate();
        }
    }

    private void OnStatusTimerTick(object? sender, EventArgs e)
    {
        UpdateLastUpdateLabel();

        if (_autoUpdateCheckBox.Checked && !_updateWorker.IsBusy)
        {
            var hoursSinceUpdate = (DateTime.Now - _lastUpdateTime).TotalHours;
            if (hoursSinceUpdate >= (double)_autoUpdateInterval.Value)
            {
                Task.Run(async () =>
                {
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(async () => await PerformUpdate()));
                    }
                    else
                    {
                        await PerformUpdate();
                    }
                });
            }
        }
    }

    private void ScheduleNextUpdate()
    {
        if (_lastUpdateTime == DateTime.MinValue)
            _lastUpdateTime = DateTime.Now;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();

            ShowBalloonTip(2000, "Minimized to Tray",
                "Application is still running in the background.", ToolTipIcon.Info);
        }
    }

    private Icon? LoadAppIcon()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("PKHeX.TemplateRegen.Resources.app.ico");
            return stream != null ? new Icon(stream) : null;
        }
        catch
        {
            return null;
        }
    }

    private void ShowBalloonTip(int timeout, string title, string text, ToolTipIcon icon)
    {
        try
        {
            _trayIcon?.ShowBalloonTip(timeout, title, text, icon);
        }
        catch (ObjectDisposedException)
        {
            // Tray icon was disposed, ignore
        }
        catch (Exception ex)
        {
            AppLogManager.LogDebug($"Error showing balloon tip: {ex.Message}");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Dispose NotifyIcon first and hide it to prevent lingering in system tray
            try
            {
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }
            }
            catch (Exception ex)
            {
                AppLogManager.LogError("Error disposing tray icon", ex);
            }

            _updateWorker?.Dispose();
            _statusTimer?.Stop();
            _statusTimer?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal class UpdateResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}

internal class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item.Selected)
        {
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(62, 62, 66)),
                new Rectangle(Point.Empty, e.Item.Size));
        }
        else
        {
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(45, 45, 48)),
                new Rectangle(Point.Empty, e.Item.Size));
        }
    }
}
