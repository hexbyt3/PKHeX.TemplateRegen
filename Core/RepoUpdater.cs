using LibGit2Sharp;

namespace PKHeX.TemplateRegen.Core;

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
