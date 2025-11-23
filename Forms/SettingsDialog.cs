namespace PKHeX.TemplateRegen;

public class SettingsDialog : Form
{
    private readonly ProgramSettings _settings;
    private TextBox _repoFolderTextBox = null!;
    private TextBox _pkHexPathTextBox = null!;
    private TextBox _evGalPathTextBox = null!;
    private TextBox _pgetPathTextBox = null!;
    private CheckBox _autoManageEvGalCheckBox = null!;
    private CheckBox _autoManagePgetCheckBox = null!;
    private Button _browsePkHexButton = null!;
    private Button _browseEvGalButton = null!;
    private Button _browsePgetButton = null!;
    private Button _browseRepoFolderButton = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;
    private Button _exportButton = null!;
    private Button _importButton = null!;
    private ComboBox _profileComboBox = null!;
    private Button _saveProfileButton = null!;
    private Button _deleteProfileButton = null!;

    public SettingsDialog(ProgramSettings settings)
    {
        _settings = settings;
        InitializeComponent();
        SetupDarkTheme();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        Text = "Settings";
        Size = new Size(700, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Profile section
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Settings section
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Buttons section

        // Profile Section
        var profilePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };

        profilePanel.Controls.Add(new Label { Text = "Profile:", AutoSize = true, Margin = new Padding(0, 5, 5, 0) });
        _profileComboBox = new ComboBox
        {
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 0, 10, 0)
        };

        _saveProfileButton = new Button
        {
            Text = "Save Profile",
            Width = 90,
            Height = 25,
            Margin = new Padding(0, 0, 5, 0)
        };

        _deleteProfileButton = new Button
        {
            Text = "Delete",
            Width = 60,
            Height = 25,
            Margin = new Padding(0, 0, 20, 0)
        };

        _importButton = new Button
        {
            Text = "Import",
            Width = 60,
            Height = 25,
            Margin = new Padding(0, 0, 5, 0)
        };

        _exportButton = new Button
        {
            Text = "Export",
            Width = 60,
            Height = 25
        };

        profilePanel.Controls.AddRange(new Control[]
        {
            new Label { Text = "Profile:", AutoSize = true, Margin = new Padding(0, 5, 5, 0) },
            _profileComboBox, _saveProfileButton, _deleteProfileButton, _importButton, _exportButton
        });

        // Settings Section
        var settingsGroup = new GroupBox
        {
            Text = "Repository Paths",
            Dock = DockStyle.Fill
        };

        var settingsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 7,
            Padding = new Padding(10)
        };
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));

        // Repo Folder
        settingsLayout.Controls.Add(new Label { Text = "Repository Folder:", Dock = DockStyle.Fill }, 0, 0);
        _repoFolderTextBox = new TextBox { Dock = DockStyle.Fill };
        settingsLayout.Controls.Add(_repoFolderTextBox, 1, 0);
        _browseRepoFolderButton = new Button { Text = "Browse", Dock = DockStyle.Fill };
        settingsLayout.Controls.Add(_browseRepoFolderButton, 2, 0);

        // Separator
        var separator = new Label
        {
            Text = "Or specify individual paths:",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9, FontStyle.Italic),
            Margin = new Padding(0, 10, 0, 10)
        };
        settingsLayout.Controls.Add(separator, 0, 1);
        settingsLayout.SetColumnSpan(separator, 3);

        // PKHeX Path
        settingsLayout.Controls.Add(new Label { Text = "PKHeX Legality Path:", Dock = DockStyle.Fill }, 0, 2);
        _pkHexPathTextBox = new TextBox { Dock = DockStyle.Fill };
        settingsLayout.Controls.Add(_pkHexPathTextBox, 1, 2);
        _browsePkHexButton = new Button { Text = "Browse", Dock = DockStyle.Fill };
        settingsLayout.Controls.Add(_browsePkHexButton, 2, 2);

        // Events Gallery Path
        settingsLayout.Controls.Add(new Label { Text = "Events Gallery Path:", Dock = DockStyle.Fill }, 0, 3);
        _evGalPathTextBox = new TextBox { Dock = DockStyle.Fill };
        settingsLayout.Controls.Add(_evGalPathTextBox, 1, 3);
        _browseEvGalButton = new Button { Text = "Browse", Dock = DockStyle.Fill };
        settingsLayout.Controls.Add(_browseEvGalButton, 2, 3);

        // Auto-manage EventsGallery checkbox
        _autoManageEvGalCheckBox = new CheckBox
        {
            Text = "Automatically clone and update EventsGallery",
            Dock = DockStyle.Fill,
            Checked = true,
            AutoSize = true,
            Margin = new Padding(0, 5, 0, 0)
        };
        settingsLayout.Controls.Add(_autoManageEvGalCheckBox, 1, 4);
        settingsLayout.SetColumnSpan(_autoManageEvGalCheckBox, 2);

        // PoGo Enc Tool Path
        settingsLayout.Controls.Add(new Label { Text = "PoGo Enc Tool Path:", Dock = DockStyle.Fill }, 0, 5);
        _pgetPathTextBox = new TextBox { Dock = DockStyle.Fill };
        settingsLayout.Controls.Add(_pgetPathTextBox, 1, 5);
        _browsePgetButton = new Button { Text = "Browse", Dock = DockStyle.Fill };
        settingsLayout.Controls.Add(_browsePgetButton, 2, 5);

        // Auto-manage PGET checkbox
        _autoManagePgetCheckBox = new CheckBox
        {
            Text = "Automatically clone, update, and build PoGoEncTool",
            Dock = DockStyle.Fill,
            Checked = true,
            AutoSize = true,
            Margin = new Padding(0, 5, 0, 0)
        };
        settingsLayout.Controls.Add(_autoManagePgetCheckBox, 1, 6);
        settingsLayout.SetColumnSpan(_autoManagePgetCheckBox, 2);

        settingsGroup.Controls.Add(settingsLayout);

        // Button Section
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };

        _cancelButton = new Button
        {
            Text = "Cancel",
            Width = 80,
            Height = 30,
            DialogResult = DialogResult.Cancel,
            Margin = new Padding(5)
        };

        _saveButton = new Button
        {
            Text = "Save",
            Width = 80,
            Height = 30,
            DialogResult = DialogResult.OK,
            Margin = new Padding(5)
        };

        buttonPanel.Controls.Add(_cancelButton);
        buttonPanel.Controls.Add(_saveButton);

        mainLayout.Controls.Add(profilePanel, 0, 0);
        mainLayout.Controls.Add(settingsGroup, 0, 1);
        mainLayout.Controls.Add(buttonPanel, 0, 2);

        Controls.Add(mainLayout);

        // Event handlers
        _browseRepoFolderButton.Click += (s, e) => BrowseFolder(_repoFolderTextBox);
        _browsePkHexButton.Click += (s, e) => BrowseFolder(_pkHexPathTextBox);
        _browseEvGalButton.Click += (s, e) => BrowseFolder(_evGalPathTextBox);
        _browsePgetButton.Click += (s, e) => BrowseFolder(_pgetPathTextBox);
        _saveButton.Click += OnSaveClick;
        _profileComboBox.SelectedIndexChanged += OnProfileChanged;
        _saveProfileButton.Click += OnSaveProfileClick;
        _deleteProfileButton.Click += OnDeleteProfileClick;
        _importButton.Click += OnImportClick;
        _exportButton.Click += OnExportClick;

        // Load profiles
        LoadProfiles();
    }

    private void SetupDarkTheme()
    {
        var darkBg = Color.FromArgb(30, 30, 30);
        var darkControl = Color.FromArgb(45, 45, 48);
        var darkBorder = Color.FromArgb(62, 62, 66);
        var lightText = Color.FromArgb(241, 241, 241);

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
            }
            else if (control is TextBox || control is ComboBox)
            {
                control.BackColor = darkControl;
                control.ForeColor = lightText;
            }
            else if (control is CheckBox)
            {
                control.ForeColor = lightText;
            }
            else if (control is GroupBox || control is Panel || control is TableLayoutPanel || control is FlowLayoutPanel)
            {
                control.BackColor = darkBg;
                control.ForeColor = lightText;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child);
            }
        }

        ApplyThemeToControl(this);
    }

    private void LoadSettings()
    {
        _repoFolderTextBox.Text = _settings.RepoFolder;
        _pkHexPathTextBox.Text = _settings.RepoPKHeXLegality;
        _evGalPathTextBox.Text = _settings.RepoNameEvGal;
        _pgetPathTextBox.Text = _settings.RepoNamePGET;
        _autoManageEvGalCheckBox.Checked = _settings.AutoManageEventsGalleryRepo;
        _autoManagePgetCheckBox.Checked = _settings.AutoManagePGETRepo;
    }

    private void LoadProfiles()
    {
        _profileComboBox.Items.Clear();
        _profileComboBox.Items.Add("Default");

        var profiles = ProfileManager.GetProfileNames();
        _profileComboBox.Items.AddRange(profiles.ToArray());

        _profileComboBox.SelectedIndex = 0;
    }

    private void BrowseFolder(TextBox targetTextBox)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select repository folder",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (!string.IsNullOrEmpty(targetTextBox.Text) && Directory.Exists(targetTextBox.Text))
        {
            dialog.InitialDirectory = targetTextBox.Text;
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            targetTextBox.Text = dialog.SelectedPath;
        }
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        _settings.RepoFolder = _repoFolderTextBox.Text;
        _settings.RepoPKHeXLegality = _pkHexPathTextBox.Text;
        _settings.RepoNameEvGal = _evGalPathTextBox.Text;
        _settings.RepoNamePGET = _pgetPathTextBox.Text;
        _settings.AutoManageEventsGalleryRepo = _autoManageEvGalCheckBox.Checked;
        _settings.AutoManagePGETRepo = _autoManagePgetCheckBox.Checked;

        // Validate paths
        var errors = new List<string>();

        if (!Directory.Exists(_settings.PathPKHeX))
            errors.Add($"PKHeX path not found: {_settings.PathPKHeX}");
        if (!Directory.Exists(_settings.PathRepoEvGal))
            errors.Add($"Events Gallery path not found: {_settings.PathRepoEvGal}");
        if (!Directory.Exists(_settings.PathRepoPGET))
            errors.Add($"PoGo Enc Tool path not found: {_settings.PathRepoPGET}");

        if (errors.Count > 0)
        {
            var result = MessageBox.Show(
                $"The following paths were not found:\n\n{string.Join("\n", errors)}\n\nSave anyway?",
                "Path Validation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.No)
            {
                DialogResult = DialogResult.None;
            }
        }
    }

    private void OnProfileChanged(object? sender, EventArgs e)
    {
        if (_profileComboBox.SelectedIndex > 0)
        {
            var profileName = _profileComboBox.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(profileName))
            {
                var profile = ProfileManager.LoadProfile(profileName);
                if (profile != null)
                {
                    _repoFolderTextBox.Text = profile.RepoFolder;
                    _pkHexPathTextBox.Text = profile.RepoPKHeXLegality;
                    _evGalPathTextBox.Text = profile.RepoNameEvGal;
                    _pgetPathTextBox.Text = profile.RepoNamePGET;
                    _autoManageEvGalCheckBox.Checked = profile.AutoManageEventsGalleryRepo;
                    _autoManagePgetCheckBox.Checked = profile.AutoManagePGETRepo;
                }
            }
        }
    }

    private void OnSaveProfileClick(object? sender, EventArgs e)
    {
        using var dialog = new InputDialog("Save Profile", "Enter profile name:");
        if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.InputText))
        {
            var profile = new ProgramSettings
            {
                RepoFolder = _repoFolderTextBox.Text,
                RepoPKHeXLegality = _pkHexPathTextBox.Text,
                RepoNameEvGal = _evGalPathTextBox.Text,
                RepoNamePGET = _pgetPathTextBox.Text,
                AutoManageEventsGalleryRepo = _autoManageEvGalCheckBox.Checked,
                AutoManagePGETRepo = _autoManagePgetCheckBox.Checked
            };

            ProfileManager.SaveProfile(dialog.InputText, profile);
            LoadProfiles();

            // Select the newly saved profile
            _profileComboBox.SelectedItem = dialog.InputText;
        }
    }

    private void OnDeleteProfileClick(object? sender, EventArgs e)
    {
        if (_profileComboBox.SelectedIndex > 0)
        {
            var profileName = _profileComboBox.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(profileName))
            {
                var result = MessageBox.Show(
                    $"Delete profile '{profileName}'?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    ProfileManager.DeleteProfile(profileName);
                    LoadProfiles();
                }
            }
        }
    }

    private void OnImportClick(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Import Settings"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var imported = SettingsManager.LoadSettingsFromFile(dialog.FileName);
                if (imported != null)
                {
                    _repoFolderTextBox.Text = imported.RepoFolder;
                    _pkHexPathTextBox.Text = imported.RepoPKHeXLegality;
                    _evGalPathTextBox.Text = imported.RepoNameEvGal;
                    _pgetPathTextBox.Text = imported.RepoNamePGET;
                    _autoManageEvGalCheckBox.Checked = imported.AutoManageEventsGalleryRepo;
                    _autoManagePgetCheckBox.Checked = imported.AutoManagePGETRepo;

                    MessageBox.Show("Settings imported successfully!", "Import",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to import settings: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnExportClick(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Export Settings",
            FileName = $"pkhex-regen-settings-{DateTime.Now:yyyyMMdd}.json"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var exportSettings = new ProgramSettings
                {
                    RepoFolder = _repoFolderTextBox.Text,
                    RepoPKHeXLegality = _pkHexPathTextBox.Text,
                    RepoNameEvGal = _evGalPathTextBox.Text,
                    RepoNamePGET = _pgetPathTextBox.Text,
                    AutoManageEventsGalleryRepo = _autoManageEvGalCheckBox.Checked,
                    AutoManagePGETRepo = _autoManagePgetCheckBox.Checked
                };

                SettingsManager.SaveSettingsToFile(exportSettings, dialog.FileName);

                MessageBox.Show("Settings exported successfully!", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export settings: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

// Simple input dialog for profile names
internal class InputDialog : Form
{
    private TextBox _textBox;
    private Button _okButton;
    private Button _cancelButton;

    public string InputText => _textBox.Text;

    public InputDialog(string title, string prompt)
    {
        Text = title;
        Size = new Size(400, 150);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var label = new Label
        {
            Text = prompt,
            Location = new Point(10, 10),
            Size = new Size(370, 20)
        };

        _textBox = new TextBox
        {
            Location = new Point(10, 35),
            Size = new Size(370, 25)
        };

        _okButton = new Button
        {
            Text = "OK",
            Location = new Point(225, 70),
            Size = new Size(75, 25),
            DialogResult = DialogResult.OK
        };

        _cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(305, 70),
            Size = new Size(75, 25),
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange(new Control[] { label, _textBox, _okButton, _cancelButton });

        // Apply dark theme
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.FromArgb(241, 241, 241);

        _textBox.BackColor = Color.FromArgb(45, 45, 48);
        _textBox.ForeColor = Color.FromArgb(241, 241, 241);

        _okButton.FlatStyle = FlatStyle.Flat;
        _okButton.BackColor = Color.FromArgb(45, 45, 48);
        _okButton.ForeColor = Color.FromArgb(241, 241, 241);

        _cancelButton.FlatStyle = FlatStyle.Flat;
        _cancelButton.BackColor = Color.FromArgb(45, 45, 48);
        _cancelButton.ForeColor = Color.FromArgb(241, 241, 241);
    }
}
