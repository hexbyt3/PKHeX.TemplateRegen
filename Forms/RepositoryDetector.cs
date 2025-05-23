using LibGit2Sharp;

namespace PKHeX.TemplateRegen;

public class RepositoryDetector
{
    private readonly List<string> _commonPaths = new()
    {
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "repos"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "GitHub"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "GitLab"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects"),
        @"C:\Source",
        @"C:\Repos",
        @"C:\Projects",
        @"C:\GitHub",
        @"D:\Source",
        @"D:\Repos",
        @"D:\Projects",
        @"D:\GitHub"
    };

    private readonly Dictionary<string, string[]> _repoIdentifiers = new()
    {
        ["PKHeX"] = ["PKHeX.Core", "PKHeX.WinForms", "PKHeX.sln", "Resources/legality"],
        ["EventsGallery"] = ["Released/Gen 9", "Released/Gen 8", "Released/Gen 7", "Released/Gen 6"],
        ["PoGoEncTool"] = ["PoGoEncTool.WinForms", "PoGoEncTool.Core", "pget.sln", "PoGoEncounterTool.sln"]
    };

    public async Task<List<DetectedRepository>> DetectRepositoriesAsync()
    {
        var results = new List<DetectedRepository>();
        var searchTasks = new List<Task<List<DetectedRepository>>>();

        foreach (var basePath in _commonPaths.Where(Directory.Exists))
        {
            searchTasks.Add(Task.Run(() => SearchDirectory(basePath)));
        }

        // Also search drives
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            var drivePath = drive.RootDirectory.FullName;
            if (!_commonPaths.Contains(drivePath))
            {
                searchTasks.Add(Task.Run(() => SearchDriveRoot(drivePath)));
            }
        }

        var allResults = await Task.WhenAll(searchTasks);
        results.AddRange(allResults.SelectMany(r => r));

        // Remove duplicates
        return results.GroupBy(r => r.Path.ToLowerInvariant())
                     .Select(g => g.First())
                     .OrderBy(r => r.Type)
                     .ThenBy(r => r.Path)
                     .ToList();
    }

    private List<DetectedRepository> SearchDirectory(string basePath, int maxDepth = 3)
    {
        var results = new List<DetectedRepository>();

        try
        {
            SearchDirectoryRecursive(basePath, results, 0, maxDepth);
        }
        catch (Exception ex)
        {
            AppLogManager.Log($"Error searching {basePath}: {ex.Message}");
        }

        return results;
    }

    private List<DetectedRepository> SearchDriveRoot(string drivePath)
    {
        var results = new List<DetectedRepository>();

        try
        {
            // Only search top-level directories on drive roots
            var directories = Directory.GetDirectories(drivePath)
                .Where(d => !IsSystemDirectory(d))
                .Take(50); // Limit to prevent excessive searching

            foreach (var dir in directories)
            {
                SearchDirectoryRecursive(dir, results, 0, 2);
            }
        }
        catch (Exception ex)
        {
            AppLogManager.Log($"Error searching drive {drivePath}: {ex.Message}");
        }

        return results;
    }

    private void SearchDirectoryRecursive(string path, List<DetectedRepository> results, int currentDepth, int maxDepth)
    {
        if (currentDepth > maxDepth)
            return;

        try
        {
            // Check if this is a git repository
            try
            {
                if (Directory.Exists(Path.Combine(path, ".git")) && Repository.IsValid(path))
                {
                    var detected = IdentifyRepository(path);
                    if (detected != null)
                    {
                        AppLogManager.LogDebug($"Found repository: {detected.Type} at {path}");
                        results.Add(detected);
                        return; // Don't search inside detected repositories
                    }
                }
            }
            catch (RepositoryNotFoundException)
            {
                // Not a repository, continue searching
            }

            // Check subdirectories
            var subdirs = Directory.GetDirectories(path)
                .Where(d => !IsSystemDirectory(d) && !d.Contains("node_modules") && !d.Contains(".git"));

            foreach (var subdir in subdirs)
            {
                SearchDirectoryRecursive(subdir, results, currentDepth + 1, maxDepth);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we can't access
            AppLogManager.LogDebug($"Access denied to: {path}");
        }
        catch (Exception ex)
        {
            AppLogManager.LogDebug($"Error searching {path}: {ex.Message}");
        }
    }

    private DetectedRepository? IdentifyRepository(string path)
    {
        foreach (var (repoType, identifiers) in _repoIdentifiers)
        {
            if (IsRepositoryType(path, identifiers))
            {
                return new DetectedRepository
                {
                    Type = repoType,
                    Path = path,
                    IsValid = true,
                    LastModified = GetLastModified(path)
                };
            }
        }

        return null;
    }

    private bool IsRepositoryType(string path, string[] identifiers)
    {
        int matchCount = 0;

        foreach (var identifier in identifiers)
        {
            var fullPath = Path.Combine(path, identifier.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                matchCount++;
            }
        }

        // Require at least half of the identifiers to match
        return matchCount >= Math.Ceiling(identifiers.Length / 2.0);
    }

    private DateTime GetLastModified(string path)
    {
        try
        {
            if (Directory.Exists(Path.Combine(path, ".git")))
            {
                using var repo = new Repository(path);
                return repo.Head.Tip.Author.When.DateTime;
            }
        }
        catch (Exception)
        {
            // If we can't read Git info, fall back to directory timestamp
        }

        return Directory.GetLastWriteTime(path);
    }

    private bool IsSystemDirectory(string path)
    {
        var dirName = Path.GetFileName(path).ToLowerInvariant();
        var systemDirs = new[]
        {
            "windows", "program files", "program files (x86)", "programdata",
            "$recycle.bin", "system volume information", "recovery", "perflogs",
            "appdata", "temp", "tmp"
        };

        return systemDirs.Any(sd => dirName.Contains(sd));
    }
}

public class DetectedRepository
{
    public string Type { get; set; } = "";
    public string Path { get; set; } = "";
    public bool IsValid { get; set; }
    public DateTime LastModified { get; set; }
}

public class AutoDetectDialog : Form
{
    private readonly List<DetectedRepository> _repositories;
    private ComboBox? _pkHexCombo;
    private ComboBox? _evGalCombo;
    private ComboBox? _pgetCombo;

    public (string pkHex, string evGal, string pget)? SelectedPaths { get; private set; }

    public AutoDetectDialog(List<DetectedRepository> repositories)
    {
        _repositories = repositories;
        InitializeComponent();
        PopulateComboBoxes();
    }

    private void InitializeComponent()
    {
        Text = "Auto-Detected Repositories";
        Size = new Size(700, 300);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(10)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var infoLabel = new Label
        {
            Text = "Select the correct repository paths from the detected options:",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10)
        };
        layout.Controls.Add(infoLabel, 0, 0);
        layout.SetColumnSpan(infoLabel, 2);

        // PKHeX
        layout.Controls.Add(new Label { Text = "PKHeX Repository:", Dock = DockStyle.Fill }, 0, 1);
        _pkHexCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        layout.Controls.Add(_pkHexCombo, 1, 1);

        // Events Gallery
        layout.Controls.Add(new Label { Text = "Events Gallery:", Dock = DockStyle.Fill }, 0, 2);
        _evGalCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        layout.Controls.Add(_evGalCombo, 1, 2);

        // PoGo Enc Tool
        layout.Controls.Add(new Label { Text = "PoGo Enc Tool:", Dock = DockStyle.Fill }, 0, 3);
        _pgetCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        layout.Controls.Add(_pgetCombo, 1, 3);

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            Width = 80,
            Height = 30,
            DialogResult = DialogResult.Cancel
        };

        var okButton = new Button
        {
            Text = "OK",
            Width = 80,
            Height = 30,
            DialogResult = DialogResult.OK
        };

        okButton.Click += OnOkClick;

        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(okButton);

        layout.Controls.Add(buttonPanel, 0, 4);
        layout.SetColumnSpan(buttonPanel, 2);

        Controls.Add(layout);

        // Apply dark theme
        ApplyDarkTheme();
    }

    private void ApplyDarkTheme()
    {
        var darkBg = Color.FromArgb(30, 30, 30);
        var darkControl = Color.FromArgb(45, 45, 48);
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
            }
            else if (control is ComboBox combo)
            {
                combo.BackColor = darkControl;
                combo.ForeColor = lightText;
            }
            else if (control is Panel || control is TableLayoutPanel || control is FlowLayoutPanel)
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

    private void PopulateComboBoxes()
    {
        // Add empty option
        _pkHexCombo!.Items.Add("");
        _evGalCombo!.Items.Add("");
        _pgetCombo!.Items.Add("");

        // Add PKHeX paths
        var pkHexRepos = _repositories.Where(r => r.Type == "PKHeX").ToList();
        foreach (var repo in pkHexRepos)
        {
            var display = $"{repo.Path} (Modified: {repo.LastModified:yyyy-MM-dd})";
            _pkHexCombo.Items.Add(new ComboBoxItem { Display = display, Value = repo.Path });
        }

        // Add Events Gallery paths
        var evGalRepos = _repositories.Where(r => r.Type == "EventsGallery").ToList();
        foreach (var repo in evGalRepos)
        {
            var display = $"{repo.Path} (Modified: {repo.LastModified:yyyy-MM-dd})";
            _evGalCombo.Items.Add(new ComboBoxItem { Display = display, Value = repo.Path });
        }

        // Add PoGo Enc Tool paths
        var pgetRepos = _repositories.Where(r => r.Type == "PoGoEncTool").ToList();
        foreach (var repo in pgetRepos)
        {
            var display = $"{repo.Path} (Modified: {repo.LastModified:yyyy-MM-dd})";
            _pgetCombo.Items.Add(new ComboBoxItem { Display = display, Value = repo.Path });
        }

        // Auto-select if only one option
        if (_pkHexCombo.Items.Count == 2) _pkHexCombo.SelectedIndex = 1;
        if (_evGalCombo.Items.Count == 2) _evGalCombo.SelectedIndex = 1;
        if (_pgetCombo.Items.Count == 2) _pgetCombo.SelectedIndex = 1;
    }

    private void OnOkClick(object? sender, EventArgs e)
    {
        var pkHexPath = "";
        var evGalPath = "";
        var pgetPath = "";

        if (_pkHexCombo!.SelectedItem is ComboBoxItem pkItem)
        {
            // For PKHeX, we need the legality subfolder
            var basePath = pkItem.Value;
            var legalityPath = Path.Combine(basePath, "PKHeX.Core", "Resources", "legality");
            if (Directory.Exists(legalityPath))
                pkHexPath = legalityPath;
        }

        if (_evGalCombo!.SelectedItem is ComboBoxItem evItem)
            evGalPath = evItem.Value;

        if (_pgetCombo!.SelectedItem is ComboBoxItem pgItem)
            pgetPath = pgItem.Value;

        SelectedPaths = (pkHexPath, evGalPath, pgetPath);
    }

    private class ComboBoxItem
    {
        public string Display { get; set; } = "";
        public string Value { get; set; } = "";
        public override string ToString() => Display;
    }
}
