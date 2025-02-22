return BuildRunner.Execute(args, build =>
{
    var gitLogin = new GitLoginInfo("faithlifebuildbot", Environment.GetEnvironmentVariable("BUILD_BOT_PASSWORD") ?? "");

    var settings = new DotNetBuildSettings
    {
        NuGetApiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY"),
        DocsSettings = new DotNetDocsSettings
        {
            GitLogin = gitLogin,
            GitAuthor = new GitAuthorInfo("Faithlife Build Bot", "faithlifebuildbot@users.noreply.github.com"),
            SourceCodeUrl = "https://github.com/Faithlife/RepoName/tree/master/src",
            GitBranchName = "docs",
            TargetDirectory = "",
        },
        PackageSettings = new DotNetPackageSettings
        {
            GitLogin = gitLogin,
            PushTagOnPublish = x => $"v{x.Version}",
        },
    };
    settings.ExtraProperties = target =>
    {
        if (DotNetBuild.GetBuildNumber(settings) is { } buildNumber)
        {
            return
            [
                ("ContinuousIntegrationBuild", "true"),
                ("OfficialBuildId", $"{DateTime.UtcNow.ToString("yyyyMMdd")}.{buildNumber}"),
            ];
        }

        return [];
    };
    build.AddDotNetTargets(settings);
});

