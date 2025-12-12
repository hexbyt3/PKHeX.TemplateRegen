using LibGit2Sharp;
using System.Diagnostics;
using System.Text;

namespace PKHeX.TemplateRegen.Core;

public class RepoUpdateResult
{
    public bool Success { get; set; }
    public bool WasUpdated { get; set; }
    public string? CommitHash { get; set; }
    public string? CommitMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

public static class RepoUpdater
{
    [ThreadStatic]
    private static int _credentialAttempts;

    /// <summary>
    /// Clones a repository from the specified URL to the target path.
    /// </summary>
    public static bool CloneRepo(string repoName, string repoUrl, string targetPath, string branch = "main")
    {
        try
        {
            if (Directory.Exists(targetPath))
            {
                if (Repository.IsValid(targetPath))
                {
                    AppLogManager.Log($"{repoName} already exists at {targetPath}");
                    return true;
                }

                AppLogManager.LogWarning($"Directory {targetPath} exists but is not a valid git repository");
                return false;
            }

            AppLogManager.Log($"Cloning {repoName} from {repoUrl} to {targetPath}...");

            _credentialAttempts = 0;

            var cloneOptions = new CloneOptions
            {
                BranchName = branch
            };

            cloneOptions.FetchOptions.OnProgress = (output) =>
            {
                try
                {
                    AppLogManager.LogDebug($"{repoName}: {output}");
                }
                catch { }
                return true;
            };

            cloneOptions.FetchOptions.OnTransferProgress = (progress) =>
            {
                try
                {
                    var percent = progress.ReceivedObjects * 100 / Math.Max(progress.TotalObjects, 1);
                    if (percent % 10 == 0) // Only log every 10%
                        AppLogManager.LogDebug($"{repoName}: Cloning... {percent}% ({progress.ReceivedObjects}/{progress.TotalObjects} objects)");
                }
                catch { }
                return true;
            };

            cloneOptions.FetchOptions.CredentialsProvider = CredentialsHandler;

            var parentDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
                AppLogManager.Log($"Created directory: {parentDir}");
            }

            Repository.Clone(repoUrl, targetPath, cloneOptions);
            AppLogManager.Log($"Successfully cloned {repoName} to {targetPath}");
            return true;
        }
        catch (LibGit2SharpException ex)
        {
            AppLogManager.LogError($"Git error cloning {repoName}: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            AppLogManager.LogError($"Unexpected error cloning {repoName}: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Clones the repository if it doesn't exist, or updates it if it does.
    /// Returns detailed information about the operation including commit hash and whether changes were made.
    /// </summary>
    public static RepoUpdateResult CloneOrUpdateRepo(string repoName, string repoUrl, string targetPath, string branch = "main")
    {
        if (!Directory.Exists(targetPath) || !Repository.IsValid(targetPath))
        {
            var cloneSuccess = CloneRepo(repoName, repoUrl, targetPath, branch);
            if (!cloneSuccess)
            {
                return new RepoUpdateResult
                {
                    Success = false,
                    WasUpdated = false,
                    ErrorMessage = $"Failed to clone {repoName}"
                };
            }

            // Get commit info after clone
            try
            {
                using var repo = new Repository(targetPath);
                var commit = repo.Head.Tip;
                return new RepoUpdateResult
                {
                    Success = true,
                    WasUpdated = true,
                    CommitHash = commit.Sha,
                    CommitMessage = commit.MessageShort
                };
            }
            catch (Exception ex)
            {
                AppLogManager.LogWarning($"Could not retrieve commit info after clone: {ex.Message}");
                return new RepoUpdateResult
                {
                    Success = true,
                    WasUpdated = true
                };
            }
        }

        return UpdateRepoWithResult(repoName, targetPath, branch);
    }

    /// <summary>
    /// Updates repository and returns detailed result information.
    /// </summary>
    private static RepoUpdateResult UpdateRepoWithResult(string repo, string path, string branch)
    {
        try
        {
            if (!Repository.IsValid(path))
            {
                return new RepoUpdateResult
                {
                    Success = false,
                    WasUpdated = false,
                    ErrorMessage = $"Invalid {repo} repository path: {path}"
                };
            }

            AppLogManager.Log($"Updating repository: {repo}");

            using var localRepo = new Repository(path);

            var status = localRepo.RetrieveStatus();
            if (status.IsDirty)
                AppLogManager.LogWarning($"Repository {repo} has uncommitted changes. Proceeding with caution.");

            var remote = localRepo.Network.Remotes["origin"];
            if (remote == null)
            {
                return new RepoUpdateResult
                {
                    Success = false,
                    WasUpdated = false,
                    ErrorMessage = $"No origin remote found for {repo}"
                };
            }

            _credentialAttempts = 0;

            var fetchOptions = new FetchOptions
            {
                OnProgress = (output) =>
                {
                    try { AppLogManager.LogDebug($"{repo}: {output}"); } catch { }
                    return true;
                },
                OnTransferProgress = (progress) =>
                {
                    try
                    {
                        var percent = progress.ReceivedObjects * 100 / Math.Max(progress.TotalObjects, 1);
                        AppLogManager.LogDebug($"{repo}: Fetching... {percent}% ({progress.ReceivedObjects}/{progress.TotalObjects} objects)");
                    }
                    catch { }
                    return true;
                },
                OnUpdateTips = (refName, oldId, newId) =>
                {
                    try { AppLogManager.LogDebug($"{repo}: Updated {refName}"); } catch { }
                    return true;
                },
                CredentialsProvider = CredentialsHandler
            };

            AppLogManager.Log($"Fetching latest changes for {repo}...");

            var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
            Commands.Fetch(localRepo, remote.Name, refSpecs, fetchOptions, "Fetching latest changes");

            var branches = localRepo.Branches;
            var localBranch = branches[branch];
            var remoteBranch = branches[$"origin/{branch}"];

            if (localBranch == null)
            {
                return new RepoUpdateResult
                {
                    Success = false,
                    WasUpdated = false,
                    ErrorMessage = $"Local branch '{branch}' not found in {repo}"
                };
            }

            if (remoteBranch == null)
            {
                return new RepoUpdateResult
                {
                    Success = false,
                    WasUpdated = false,
                    ErrorMessage = $"Remote branch 'origin/{branch}' not found in {repo}"
                };
            }

            var localCommit = localBranch.Tip;
            var remoteCommit = remoteBranch.Tip;

            if (localCommit.Sha == remoteCommit.Sha)
            {
                AppLogManager.Log($"{repo} is already up to date");
                return new RepoUpdateResult
                {
                    Success = true,
                    WasUpdated = false,
                    CommitHash = remoteCommit.Sha,
                    CommitMessage = remoteCommit.MessageShort
                };
            }

            var divergence = localRepo.ObjectDatabase.CalculateHistoryDivergence(localCommit, remoteCommit);
            AppLogManager.Log($"{repo} is {divergence.BehindBy} commits behind origin/{branch}");

            AppLogManager.Log($"Updating {repo} to latest commit...");

            var checkoutOptions = new CheckoutOptions
            {
                CheckoutModifiers = CheckoutModifiers.Force,
                OnCheckoutProgress = (path, completedSteps, totalSteps) =>
                {
                    try
                    {
                        var percent = completedSteps * 100 / Math.Max(totalSteps, 1);
                        if (percent % 10 == 0)
                            AppLogManager.LogDebug($"{repo}: Checking out files... {percent}%");
                    }
                    catch { }
                }
            };

            Commands.Checkout(localRepo, localBranch, checkoutOptions);
            localRepo.Reset(ResetMode.Hard, remoteCommit);

            AppLogManager.Log($"Successfully updated {repo} to commit {remoteCommit.Sha[..7]} by {remoteCommit.Author.Name}");
            AppLogManager.Log($"Commit message: {remoteCommit.MessageShort}");

            return new RepoUpdateResult
            {
                Success = true,
                WasUpdated = true,
                CommitHash = remoteCommit.Sha,
                CommitMessage = remoteCommit.MessageShort
            };
        }
        catch (LibGit2SharpException ex)
        {
            AppLogManager.LogError($"Git error updating {repo}: {ex.Message}");
            return new RepoUpdateResult
            {
                Success = false,
                WasUpdated = false,
                ErrorMessage = ex.Message
            };
        }
        catch (Exception ex)
        {
            AppLogManager.LogError($"Unexpected error updating {repo}: {ex.Message}", ex);
            return new RepoUpdateResult
            {
                Success = false,
                WasUpdated = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Builds a .NET project using dotnet CLI.
    /// </summary>
    public static bool BuildProject(string repoName, string projectPath, string configuration = "Release")
    {
        try
        {
            AppLogManager.Log($"Building {repoName} ({configuration} configuration)...");

            string searchPath = projectPath;
            string? csprojFile = null;

            // If projectPath is a .csproj file, use it directly
            if (File.Exists(projectPath) && projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                csprojFile = projectPath;
                searchPath = Path.GetDirectoryName(projectPath) ?? projectPath;
            }
            else if (Directory.Exists(projectPath))
            {
                // Look for .csproj files in the directory
                var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.TopDirectoryOnly);
                if (csprojFiles.Length == 0)
                {
                    // Search in subdirectories
                    csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories);
                }

                if (csprojFiles.Length == 0)
                {
                    AppLogManager.LogError($"No .csproj file found in {projectPath}");
                    return false;
                }

                // Prefer WinForms project if available
                csprojFile = csprojFiles.FirstOrDefault(f => f.Contains("WinForms")) ?? csprojFiles[0];
                searchPath = Path.GetDirectoryName(csprojFile) ?? projectPath;
            }
            else
            {
                AppLogManager.LogError($"Project path not found: {projectPath}");
                return false;
            }

            AppLogManager.Log($"Building project: {Path.GetFileName(csprojFile)}");

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{csprojFile}\" -c {configuration}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = searchPath
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                AppLogManager.LogError("Failed to start dotnet build process");
                return false;
            }

            var output = new List<string>();
            var errors = new List<string>();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.Add(e.Data);
                    AppLogManager.LogDebug($"Build: {e.Data}");
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errors.Add(e.Data);
                    AppLogManager.LogDebug($"Build Error: {e.Data}");
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            var exitCode = process.ExitCode;
            if (exitCode == 0)
            {
                AppLogManager.Log($"Successfully built {repoName}");
                return true;
            }
            else
            {
                AppLogManager.LogError($"Build failed with exit code {exitCode}");
                if (errors.Any())
                {
                    AppLogManager.LogError($"Build errors: {string.Join(Environment.NewLine, errors)}");
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            AppLogManager.LogError($"Error building {repoName}: {ex.Message}", ex);
            return false;
        }
    }

    public static bool UpdateRepo(string repo, string path, string branch)
    {
        try
        {
            if (!Repository.IsValid(path))
            {
                AppLogManager.LogError($"Invalid {repo} repository path: {path}");
                return false;
            }

            AppLogManager.Log($"Updating repository: {repo}");

            using var localRepo = new Repository(path);

            var status = localRepo.RetrieveStatus();
            if (status.IsDirty)
                AppLogManager.LogWarning($"Repository {repo} has uncommitted changes. Proceeding with caution.");

            var remote = localRepo.Network.Remotes["origin"];
            if (remote == null)
            {
                AppLogManager.LogError($"No origin remote found for {repo}");
                return false;
            }

            _credentialAttempts = 0;

            var fetchOptions = new FetchOptions
            {
                OnProgress = (output) =>
                {
                    try
                    {
                        AppLogManager.LogDebug($"{repo}: {output}");
                    }
                    catch { }
                    return true;
                },
                OnTransferProgress = (progress) =>
                {
                    try
                    {
                        var percent = progress.ReceivedObjects * 100 / Math.Max(progress.TotalObjects, 1);
                        AppLogManager.LogDebug($"{repo}: Fetching... {percent}% ({progress.ReceivedObjects}/{progress.TotalObjects} objects)");
                    }
                    catch { }
                    return true;
                },
                OnUpdateTips = (refName, oldId, newId) =>
                {
                    try
                    {
                        AppLogManager.LogDebug($"{repo}: Updated {refName}");
                    }
                    catch { }
                    return true;
                },
                CredentialsProvider = CredentialsHandler
            };

            AppLogManager.Log($"Fetching latest changes for {repo}...");

            var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
            Commands.Fetch(localRepo, remote.Name, refSpecs, fetchOptions, "Fetching latest changes");

            var branches = localRepo.Branches;
            var localBranch = branches[branch];
            var remoteBranch = branches[$"origin/{branch}"];

            if (localBranch == null)
            {
                AppLogManager.LogError($"Local branch '{branch}' not found in {repo}");
                return false;
            }

            if (remoteBranch == null)
            {
                AppLogManager.LogError($"Remote branch 'origin/{branch}' not found in {repo}");
                return false;
            }

            var localCommit = localBranch.Tip;
            var remoteCommit = remoteBranch.Tip;

            if (localCommit.Sha == remoteCommit.Sha)
            {
                AppLogManager.Log($"{repo} is already up to date");
                return true;
            }

            var divergence = localRepo.ObjectDatabase.CalculateHistoryDivergence(localCommit, remoteCommit);
            AppLogManager.Log($"{repo} is {divergence.BehindBy} commits behind origin/{branch}");

            AppLogManager.Log($"Updating {repo} to latest commit...");

            var checkoutOptions = new CheckoutOptions
            {
                CheckoutModifiers = CheckoutModifiers.Force,
                OnCheckoutProgress = (path, completedSteps, totalSteps) =>
                {
                    try
                    {
                        var percent = completedSteps * 100 / Math.Max(totalSteps, 1);
                        if (percent % 10 == 0) // Only log every 10%
                            AppLogManager.LogDebug($"{repo}: Checking out files... {percent}%");
                    }
                    catch { }
                }
            };

            Commands.Checkout(localRepo, localBranch, checkoutOptions);
            localRepo.Reset(ResetMode.Hard, remoteCommit);

            AppLogManager.Log($"Successfully updated {repo} to commit {remoteCommit.Sha[..7]} by {remoteCommit.Author.Name}");
            AppLogManager.Log($"Commit message: {remoteCommit.MessageShort}");

            return true;
        }
        catch (LibGit2SharpException ex)
        {
            AppLogManager.LogError($"Git error updating {repo}: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            AppLogManager.LogError($"Unexpected error updating {repo}: {ex.Message}", ex);
            return false;
        }
    }

    private static Credentials CredentialsHandler(string url, string usernameFromUrl, SupportedCredentialTypes types)
    {
        // Prevent infinite retry loops - only attempt credentials once
        _credentialAttempts++;

        // For public HTTPS repositories (like GitHub public repos), use anonymous credentials
        // This prevents authentication errors while allowing read access
        if (url.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // Return default credentials which works for public repos
            return new DefaultCredentials();
        }

        // For SSH or other authenticated scenarios
        if (types.HasFlag(SupportedCredentialTypes.Default))
        {
            return new DefaultCredentials();
        }

        // Default fallback
        return new DefaultCredentials();
    }

    /// <summary>
    /// Clones or updates a repository using Git CLI.
    /// This method is preferred for repositories with long paths on Windows,
    /// as it respects the core.longpaths Git configuration.
    /// </summary>
    public static RepoUpdateResult CloneOrUpdateRepoViaCli(string repoName, string repoUrl, string targetPath, string branch = "main")
    {
        // First, ensure long paths are enabled in git config
        EnsureLongPathsEnabled();

        if (!Directory.Exists(targetPath) || !IsGitRepository(targetPath))
        {
            return CloneRepoViaCli(repoName, repoUrl, targetPath, branch);
        }

        return UpdateRepoViaCli(repoName, targetPath, branch);
    }

    /// <summary>
    /// Ensures Git is configured to handle long paths on Windows.
    /// </summary>
    private static void EnsureLongPathsEnabled()
    {
        try
        {
            var result = RunGitCommand(".", "config --global core.longpaths true", timeoutMs: 5000);
            if (!result.Success)
            {
                AppLogManager.LogWarning("Could not enable Git long paths. You may need to run: git config --global core.longpaths true");
            }
        }
        catch (Exception ex)
        {
            AppLogManager.LogWarning($"Could not configure Git long paths: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a directory is a valid Git repository using Git CLI.
    /// </summary>
    private static bool IsGitRepository(string path)
    {
        if (!Directory.Exists(path))
            return false;

        var result = RunGitCommand(path, "rev-parse --is-inside-work-tree", timeoutMs: 5000);
        return result.Success && result.Output.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Clones a repository using Git CLI.
    /// </summary>
    private static RepoUpdateResult CloneRepoViaCli(string repoName, string repoUrl, string targetPath, string branch)
    {
        try
        {
            if (Directory.Exists(targetPath))
            {
                if (IsGitRepository(targetPath))
                {
                    AppLogManager.Log($"{repoName} already exists at {targetPath}");
                    return GetCurrentCommitInfo(repoName, targetPath, false);
                }

                AppLogManager.LogWarning($"Directory {targetPath} exists but is not a valid git repository");
                return new RepoUpdateResult
                {
                    Success = false,
                    WasUpdated = false,
                    ErrorMessage = $"Directory exists but is not a valid git repository: {targetPath}"
                };
            }

            var parentDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
                AppLogManager.Log($"Created directory: {parentDir}");
            }

            AppLogManager.Log($"Cloning {repoName} from {repoUrl} to {targetPath}...");
            AppLogManager.Log("This may take a few minutes for large repositories...");

            var cloneArgs = $"clone --branch {branch} --single-branch \"{repoUrl}\" \"{targetPath}\"";
            var result = RunGitCommand(parentDir ?? ".", cloneArgs, timeoutMs: 600000); // 10 minute timeout for clone

            if (!result.Success)
            {
                AppLogManager.LogError($"Failed to clone {repoName}: {result.Error}");
                return new RepoUpdateResult
                {
                    Success = false,
                    WasUpdated = false,
                    ErrorMessage = result.Error
                };
            }

            AppLogManager.Log($"Successfully cloned {repoName} to {targetPath}");
            return GetCurrentCommitInfo(repoName, targetPath, true);
        }
        catch (Exception ex)
        {
            AppLogManager.LogError($"Error cloning {repoName}: {ex.Message}", ex);
            return new RepoUpdateResult
            {
                Success = false,
                WasUpdated = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Updates a repository using Git CLI (fetch + reset --hard).
    /// </summary>
    private static RepoUpdateResult UpdateRepoViaCli(string repoName, string targetPath, string branch)
    {
        try
        {
            if (!IsGitRepository(targetPath))
            {
                return new RepoUpdateResult
                {
                    Success = false,
                    WasUpdated = false,
                    ErrorMessage = $"Invalid {repoName} repository path: {targetPath}"
                };
            }

            AppLogManager.Log($"Updating repository: {repoName}");

            // Get current commit hash before update
            var beforeResult = RunGitCommand(targetPath, "rev-parse HEAD", timeoutMs: 5000);
            var beforeHash = beforeResult.Success ? beforeResult.Output.Trim() : null;

            // Fetch latest changes
            AppLogManager.Log($"Fetching latest changes for {repoName}...");
            var fetchResult = RunGitCommand(targetPath, "fetch origin", timeoutMs: 300000); // 5 minute timeout

            if (!fetchResult.Success)
            {
                AppLogManager.LogError($"Failed to fetch {repoName}: {fetchResult.Error}");
                return new RepoUpdateResult
                {
                    Success = false,
                    WasUpdated = false,
                    ErrorMessage = fetchResult.Error
                };
            }

            // Check if there are updates
            var remoteRef = $"origin/{branch}";
            var diffResult = RunGitCommand(targetPath, $"rev-list HEAD..{remoteRef} --count", timeoutMs: 10000);
            var commitsBehind = 0;
            if (diffResult.Success && int.TryParse(diffResult.Output.Trim(), out var behind))
            {
                commitsBehind = behind;
            }

            if (commitsBehind == 0)
            {
                AppLogManager.Log($"{repoName} is already up to date");
                return GetCurrentCommitInfo(repoName, targetPath, false);
            }

            AppLogManager.Log($"{repoName} is {commitsBehind} commits behind {remoteRef}");
            AppLogManager.Log($"Updating {repoName} to latest commit...");

            // Reset to remote branch (handles long paths better than checkout)
            var resetResult = RunGitCommand(targetPath, $"reset --hard {remoteRef}", timeoutMs: 300000);

            if (!resetResult.Success)
            {
                AppLogManager.LogError($"Failed to update {repoName}: {resetResult.Error}");
                return new RepoUpdateResult
                {
                    Success = false,
                    WasUpdated = false,
                    ErrorMessage = resetResult.Error
                };
            }

            var commitInfo = GetCurrentCommitInfo(repoName, targetPath, true);
            AppLogManager.Log($"Successfully updated {repoName} to commit {commitInfo.CommitHash?[..7]}");

            return commitInfo;
        }
        catch (Exception ex)
        {
            AppLogManager.LogError($"Error updating {repoName}: {ex.Message}", ex);
            return new RepoUpdateResult
            {
                Success = false,
                WasUpdated = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets the current commit information from a repository.
    /// </summary>
    private static RepoUpdateResult GetCurrentCommitInfo(string repoName, string targetPath, bool wasUpdated)
    {
        try
        {
            var hashResult = RunGitCommand(targetPath, "rev-parse HEAD", timeoutMs: 5000);
            var msgResult = RunGitCommand(targetPath, "log -1 --format=%s", timeoutMs: 5000);

            return new RepoUpdateResult
            {
                Success = true,
                WasUpdated = wasUpdated,
                CommitHash = hashResult.Success ? hashResult.Output.Trim() : null,
                CommitMessage = msgResult.Success ? msgResult.Output.Trim() : null
            };
        }
        catch
        {
            return new RepoUpdateResult
            {
                Success = true,
                WasUpdated = wasUpdated
            };
        }
    }

    /// <summary>
    /// Runs a Git command and returns the result.
    /// </summary>
    private static (bool Success, string Output, string Error) RunGitCommand(string workingDirectory, string arguments, int timeoutMs = 120000)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return (false, "", "Failed to start git process");
            }

            var output = new StringBuilder();
            var error = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    output.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    error.AppendLine(e.Data);
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!process.WaitForExit(timeoutMs))
            {
                try { process.Kill(); } catch { }
                return (false, output.ToString(), "Git command timed out");
            }

            var success = process.ExitCode == 0;
            return (success, output.ToString(), error.ToString());
        }
        catch (Exception ex)
        {
            return (false, "", ex.Message);
        }
    }
}
