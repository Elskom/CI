using System;
using System.Reflection;
using Octokit;

namespace CI
{
    internal class Program
    {
        internal static GitHubClient client;

        private static void Main(string[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            Console.Write("Logging into github application...");
            client = new GitHubClient(new ProductHeaderValue("CI", Assembly.GetEntryAssembly().GetName().Version.ToString()))
            {
                Credentials = new Credentials(GitHubAppToken.AppToken, AuthenticationType.Bearer)
            };
            Console.WriteLine(" Done.");
            do
            {
                // prevent application from closing.
            }
            while (true);
        }
    }
}
