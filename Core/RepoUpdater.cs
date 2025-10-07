using LibGit2Sharp;
using System.Diagnostics;

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
        return new DefaultCredentials();
    }
}
