using LibGit2Sharp;

namespace PKHeX.TemplateRegen;

public static class RepoUpdater
{
    public static bool UpdateRepo(string repo, string path, string branch)
    {
        try
        {
            if (!Repository.IsValid(path))
                throw new OperationCanceledException($"Invalid {repo} path.");

            LogUtil.Log($"Updating repo: {repo}");
            using var localRepo = new Repository(path);
            var remote = localRepo.Network.Remotes["origin"];
            var fetchOptions = new FetchOptions();

            const string msg = "Fetching latest changes...";
            LogUtil.Log(msg);
            Commands.Fetch(localRepo, remote.Name, remote.FetchRefSpecs.Select(x => x.Specification), fetchOptions, msg);
            var branches = localRepo.Branches;
            var remoteBranch = branches[$"origin/{branch}"] ?? throw new OperationCanceledException($"""Remote branch "{branch}" not found.""");

            LogUtil.Log($"Resetting local branch to remote branch: {branch}");
            Commands.Checkout(localRepo, branches[branch]);
            localRepo.Reset(ResetMode.Hard, remoteBranch.Tip);
        }
        catch (Exception ex)
        {
            LogUtil.Log($"Error: {ex.Message}");
            return false;
        }

        LogUtil.Log($"Updated repo: {repo}");
        return true;
    }
}
