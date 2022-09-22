using System.Reflection;
using CI;
using Octokit;
using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddSingleton(
        new GitHubClient(
            new ProductHeaderValue(
                "CI",
                Assembly.GetEntryAssembly()!.GetName().Version!.ToString()))
        {
            Credentials = new Credentials(
                Environment.GetEnvironmentVariable("GITHUB_APPLICATION_TOKEN"),
                AuthenticationType.Bearer)
        })
    .AddSingleton<WebhookEventProcessor, CIWebhookEventProcessor>();
Console.Write("Logging into github application...");
var app = builder.Build();
Console.WriteLine(" Done.");
app.UseRouting()
    .UseEndpoints(endpoints => endpoints.MapGitHubWebhooks());

app.Run();
