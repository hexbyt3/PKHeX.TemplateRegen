using LibGit2Sharp;

namespace PKHeX.TemplateRegen;

public static class RepoUpdater
{
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

            // Check current status
            var status = localRepo.RetrieveStatus();
            if (status.IsDirty)
            {
                AppLogManager.LogWarning($"Repository {repo} has uncommitted changes. Proceeding with caution.");
            }

            var remote = localRepo.Network.Remotes["origin"];
            if (remote == null)
            {
                AppLogManager.LogError($"No origin remote found for {repo}");
                return false;
            }

            // Configure fetch options with progress reporting
            var fetchOptions = new FetchOptions
            {
                OnProgress = (output) =>
                {
                    AppLogManager.LogDebug($"{repo}: {output}");
                    return true;
                },
                OnTransferProgress = (progress) =>
                {
                    var percent = (progress.ReceivedObjects * 100) / Math.Max(progress.TotalObjects, 1);
                    AppLogManager.LogDebug($"{repo}: Fetching... {percent}% ({progress.ReceivedObjects}/{progress.TotalObjects} objects)");
                    return true;
                },
                OnUpdateTips = (refName, oldId, newId) =>
                {
                    AppLogManager.LogDebug($"{repo}: Updated {refName}");
                    return true;
                },
                CredentialsProvider = CredentialsHandler
            };

            AppLogManager.Log($"Fetching latest changes for {repo}...");

            var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
            Commands.Fetch(localRepo, remote.Name, refSpecs, fetchOptions, "Fetching latest changes");

            // Check if we need to update
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

            // Compare commits
            var localCommit = localBranch.Tip;
            var remoteCommit = remoteBranch.Tip;

            if (localCommit.Sha == remoteCommit.Sha)
            {
                AppLogManager.Log($"{repo} is already up to date");
                return true;
            }

            // Count commits behind
            var divergence = localRepo.ObjectDatabase.CalculateHistoryDivergence(localCommit, remoteCommit);
            AppLogManager.Log($"{repo} is {divergence.BehindBy} commits behind origin/{branch}");

            // Perform the update
            AppLogManager.Log($"Updating {repo} to latest commit...");

            var checkoutOptions = new CheckoutOptions
            {
                CheckoutModifiers = CheckoutModifiers.Force,
                OnCheckoutProgress = (path, completedSteps, totalSteps) =>
                {
                    var percent = (completedSteps * 100) / Math.Max(totalSteps, 1);
                    AppLogManager.LogDebug($"{repo}: Checking out files... {percent}%");
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
        // For public repositories, we don't need credentials
        // If needed in the future, this can be extended to support authentication
        return new DefaultCredentials();
    }

    public static bool IsRepositoryUpToDate(string repo, string path, string branch)
    {
        try
        {
            if (!Repository.IsValid(path))
                return false;

            using var localRepo = new Repository(path);

            var localBranch = localRepo.Branches[branch];
            var remoteBranch = localRepo.Branches[$"origin/{branch}"];

            if (localBranch == null || remoteBranch == null)
                return false;

            return localBranch.Tip.Sha == remoteBranch.Tip.Sha;
        }
        catch
        {
            return false;
        }
    }

    public static (bool needsUpdate, string info) CheckRepositoryStatus(string repo, string path, string branch)
    {
        try
        {
            if (!Repository.IsValid(path))
                return (true, "Invalid repository");

            using var localRepo = new Repository(path);

            var localBranch = localRepo.Branches[branch];
            var remoteBranch = localRepo.Branches[$"origin/{branch}"];

            if (localBranch == null)
                return (true, $"Local branch '{branch}' not found");

            if (remoteBranch == null)
                return (true, $"Remote branch 'origin/{branch}' not found");

            if (localBranch.Tip.Sha == remoteBranch.Tip.Sha)
                return (false, "Up to date");

            var divergence = localRepo.ObjectDatabase.CalculateHistoryDivergence(localBranch.Tip, remoteBranch.Tip);
            return (true, $"{divergence.BehindBy} commits behind");
        }
        catch (Exception ex)
        {
            return (true, $"Error: {ex.Message}");
        }
    }
}
