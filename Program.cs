using System.Reflection;
using CI;
using Octokit;
using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddSingleton(
        (provider) =>
        {
            Console.Write("Logging into github application...");
            var client = new GitHubClient(
                new ProductHeaderValue(
                    "CI",
                    Assembly.GetEntryAssembly()!.GetName().Version!.ToString()))
            {
                Credentials = new Credentials(
                builder.Configuration.GetSection("GithubApplicationToken").Value,
                AuthenticationType.Bearer)
            };
            Console.WriteLine(" Done.");
            return client;
        })
    .AddSingleton<WebhookEventProcessor, CIWebhookEventProcessor>();
var app = builder.Build();
app.UseRouting()
    .UseEndpoints(endpoints => endpoints.MapGitHubWebhooks());

app.Run();
