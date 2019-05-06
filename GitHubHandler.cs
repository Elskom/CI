using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.WebHooks;
using Newtonsoft.Json.Linq;
using Octokit;

namespace CI
{
    public class GitHubHandler : WebHookHandler
    {
        public override Task ExecuteAsync(string receiver, WebHookHandlerContext context)
        {
            var action = context.Actions.First();
            var data = context.GetDataOrDefault<JObject>();

            // TODO: Ensure that issue is a pull request.
            if (action == "opened")
            {
                this.Process_pull_requestAsync(data["pull_request"]);
            }
            // handle reopened action too.
            else if (action == "reopened")
            {
                this.Process_pull_requestAsync(data["pull_request"]);
            }
            else if (action == "closed")
            {
                this.Process_pull_request_closed(data["pull_request"]);
            }
            else if (action == "labeled")
            {
                this.Process_pull_requestAsync(data["pull_request"]);
            }
            else if (action == "unlabeled")
            {
                this.Process_pull_requestAsync(data["pull_request"]);
            }

            return Task.FromResult(true);
        }

        private void Process_pull_request_closed(JToken pull_request)
        {
            // todo: possibly check if pull requesed branch
            // is in the same repository as
            // pull_request["base"]["repo"]
            // and if it is comment on closed pull request
            // if the repo owner, admins, pull requestee
            // wants it deleted or not if the CI has
            // permision to.
            if (pull_request["head"]["repo"].Value<string>() == pull_request["base"]["repo"].Value<string>())
            {
            }
        }

        private async void Process_pull_requestAsync(JToken pull_request)
        {
            // todo: read comments from repo /org admins though for things like
            // "I approve this" and then approve the changes and then wait for
            // "merge squashed/rebased/commit" (if enabled on repository).

            // seems I cannot create a status yet???
            var newCheckRun = new NewCheckRun("Misc/NEWS", pull_request["head"]["sha"].Value<string>())
            {
                Status = CheckStatus.InProgress
            };
            var check = await Program.client.Check.Run.Create(pull_request["base"]["repo"]["owner"].Value<string>(), pull_request["base"]["repo"]["name"].Value<string>(), newCheckRun);
            var _issue = await Program.client.Issue.Get(pull_request["base"]["repo"]["owner"].Value<string>(), pull_request["base"]["repo"]["name"].Value<string>(), Convert.ToInt32(pull_request["number"].Value<string>()));
            foreach (var label in _issue.Labels)
            {
                if (label.Name != "skip news")
                {
                    var _newCheckRunOutput = new NewCheckRunOutput("Misc/NEWS", "'skip news' label found!");
                    var _checkRunUpdate = new CheckRunUpdate
                    {
                        Status = CheckStatus.Completed,
                        Output = _newCheckRunOutput,
                        Conclusion = CheckConclusion.Success
                    };
                    _ = await Program.client.Check.Run.Update(pull_request["base"]["repo"]["owner"].Value<string>(), pull_request["base"]["repo"]["name"].Value<string>(), check.Id, _checkRunUpdate);
                    return;// label.Name == "skip news";
                }
            }
            var files = await Program.client.PullRequest.Files(pull_request["base"]["repo"]["owner"].Value<string>(), pull_request["base"]["repo"]["name"].Value<string>(), Convert.ToInt32(pull_request["number"].Value<string>()));
            foreach (var file in files)
            {
                if (file.FileName.StartsWith("Misc/NEWS"))
                {
                    var _newCheckRunOutput = new NewCheckRunOutput("Misc/NEWS", "Misc/NEWS entry found!");
                    var _checkRunUpdate = new CheckRunUpdate
                    {
                        Status = CheckStatus.Completed,
                        Output = _newCheckRunOutput,
                        Conclusion = CheckConclusion.Success
                    };
                    _ = await Program.client.Check.Run.Update(pull_request["base"]["repo"]["owner"].Value<string>(), pull_request["base"]["repo"]["name"].Value<string>(), check.Id, _checkRunUpdate);
                    return; // !file[:filename].starts_with ? ("Misc/NEWS");
                }
            }

            var newCheckRunOutput = new NewCheckRunOutput("Misc/NEWS", "Misc/NEWS entry not found and 'skip news' is not added!");
            var checkRunUpdate = new CheckRunUpdate
            {
                Status = CheckStatus.Completed,
                Output = newCheckRunOutput,
                Conclusion = CheckConclusion.Failure
            };
            _ = await Program.client.Check.Run.Update(pull_request["base"]["repo"]["owner"].Value<string>(), pull_request["base"]["repo"]["name"].Value<string>(), check.Id, checkRunUpdate);
        }
    }
}
