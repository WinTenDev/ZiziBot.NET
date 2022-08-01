using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WinTenDev.Zizi.Services.Extensions;

public static class WebHookGitHubExtension
{
    public static async Task<WebHookResult> ProcessGithubWebHook(
        this WebHookService webHookService,
        HttpRequest request
    )
    {
        var result = new WebHookResult
        {
            WebhookSource = WebhookSource.GitHub
        };

        var eventName = request.Headers.FirstOrDefault(
            pair =>
                pair.Key.Contains("event", StringComparison.InvariantCultureIgnoreCase)
        ).Value;

        var bodyString = await request.GetRawBodyAsync();
        var githubRoot = bodyString.MapObject<GithubRoot>();

        var message = eventName switch
        {
            {} when githubRoot.Hook != null => $"WebHook berhasil terpasang",
            {} when githubRoot.Commits != null => githubRoot.ParseCommits(),
            {} when githubRoot.PullRequest != null => githubRoot.ExtractPullRequest(),
            {} when eventName == "star" => githubRoot.StarringRepo(),
            _ => "Event ini belum didukung. Silahkan hubungi developer. EventName: " + eventName,
        };

        result.ParsedMessage = message;

        return result;
    }

    private static string ParseCommits(this GithubRoot githubRoot)
    {
        var repository = githubRoot.Repository;
        var commits = githubRoot.Commits;
        var commitCount = commits.Count;

        var htmlMessage = HtmlMessage.Empty
            .Bold($"🏗 {commitCount} commit to ").Url(repository.HtmlUrl.ToString(), repository.FullName).Br().Br();

        commits.ForEach(
            commit => {
                htmlMessage.Url(commit.Url.ToString(), commit.Id.Substring(0, 7))
                    .Text(": ")
                    .TextBr($"{commit.Message} by {commit.Author.Name}");
            }
        );

        return htmlMessage.ToString();
    }

    private static string StarringRepo(this GithubRoot githubRoot)
    {
        var repository = githubRoot.Repository;
        var htmlMessage = HtmlMessage.Empty
            .Bold("🌟 Starring ").Url(repository.HtmlUrl.ToString(), repository.FullName).Br().Br();

        return htmlMessage.ToString();
    }

    private static string ExtractPullRequest(this GithubRoot githubRoot)
    {
        var repository = githubRoot.Repository;
        var pullRequest = githubRoot.PullRequest;

        var htmlMessage = HtmlMessage.Empty
            .Bold($"🔌 Pull Request to ").Url(repository.HtmlUrl.ToString(), repository.FullName).Br().Br()
            .Bold("Title: ").Url(pullRequest.HtmlUrl.ToString(), pullRequest.Title).Br().Br()
            .TextBr(pullRequest.Body);

        return htmlMessage.ToString();
    }
}