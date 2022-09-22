namespace CI;

using Octokit;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.PullRequest;
using PullRequest = Octokit.Webhooks.Models.PullRequestEvent.PullRequest;

public class CIWebhookEventProcessor : WebhookEventProcessor
{
    private readonly GitHubClient _client;
    private readonly ILogger<CIWebhookEventProcessor> _logger;

    public CIWebhookEventProcessor(GitHubClient client, ILogger<CIWebhookEventProcessor> logger)
    {
        this._client = client;
        this._logger = logger;
    }

    protected override async Task ProcessPullRequestWebhookAsync(
        WebhookHeaders headers,
        PullRequestEvent pullRequestEvent, PullRequestAction action)
    {
        switch (action)
        {
            case PullRequestActionValue.Opened:
            case PullRequestActionValue.Reopened:
            case PullRequestActionValue.Labeled:
            case PullRequestActionValue.Unlabeled:
            case PullRequestActionValue.Synchronize:
            {
                await this.Process_pull_requestAsync(pullRequestEvent.PullRequest).ConfigureAwait(false);
                break;
            }
            default:
            {
                break;
            }
        }
    }

    private async Task Process_pull_requestAsync(PullRequest pullRequest)
    {
        // todo: read comments from repo /org admins though for things like
        // "I approve this" and then approve the changes and then wait for
        // "merge squashed/rebased/commit" (if enabled on repository).

        // seems I cannot create a status yet???
        this._logger.LogDebug("Creating a new status check.");
        var newCheckRun = new NewCheckRun("Misc/NEWS", pullRequest.Head.Sha)
        {
            Status = CheckStatus.InProgress
        };
        var check = await this._client.Check.Run.Create(
            pullRequest.Base.Repo.Owner.Name,
            pullRequest.Base.Repo.Name,
            newCheckRun).ConfigureAwait(false);
        var issue = await this._client.Issue.Get(
            pullRequest.Base.Repo.Owner.Name,
            pullRequest.Base.Repo.Name,
            Convert.ToInt32(pullRequest.Number)).ConfigureAwait(false);
        NewCheckRunOutput newCheckRunOutput;
        CheckRunUpdate checkRunUpdate;
        if (issue.Labels.Any(label => label.Name != "skip news"))
        {
            newCheckRunOutput = new NewCheckRunOutput("Misc/NEWS", "'skip news' label found!");
            checkRunUpdate = new CheckRunUpdate
            {
                Status = CheckStatus.Completed,
                Output = newCheckRunOutput,
                Conclusion = CheckConclusion.Success
            };
            this._logger.LogDebug("Check success.");
            _ = await this._client.Check.Run.Update(
                pullRequest.Base.Repo.Owner.Name,
                pullRequest.Base.Repo.Name,
                check.Id,
                checkRunUpdate).ConfigureAwait(false);
            return;// label.Name == "skip news";
        }
        var files = await this._client.PullRequest.Files(
            pullRequest.Base.Repo.Owner.Name,
            pullRequest.Base.Repo.Name,
            Convert.ToInt32(pullRequest.Number)).ConfigureAwait(false);
        if (files.Any(file => file.FileName.StartsWith("Misc/NEWS")))
        {
            newCheckRunOutput = new NewCheckRunOutput("Misc/NEWS", "Misc/NEWS entry found!");
            checkRunUpdate = new CheckRunUpdate
            {
                Status = CheckStatus.Completed,
                Output = newCheckRunOutput,
                Conclusion = CheckConclusion.Success
            };
            this._logger.LogDebug("Check success.");
            _ = await this._client.Check.Run.Update(
                pullRequest.Base.Repo.Owner.Name,
                pullRequest.Base.Repo.Name,
                check.Id,
                checkRunUpdate).ConfigureAwait(false);
            return; // !file[:filename].starts_with ? ("Misc/NEWS");
        }

        newCheckRunOutput = new NewCheckRunOutput("Misc/NEWS", "Misc/NEWS entry not found and 'skip news' is not added!");
        checkRunUpdate = new CheckRunUpdate
        {
            Status = CheckStatus.Completed,
            Output = newCheckRunOutput,
            Conclusion = CheckConclusion.Failure
        };
        this._logger.LogDebug("Check Failure.");
        _ = await this._client.Check.Run.Update(
            pullRequest.Base.Repo.Owner.Name,
            pullRequest.Base.Repo.Name,
            check.Id,
            checkRunUpdate).ConfigureAwait(false);
    }
}
