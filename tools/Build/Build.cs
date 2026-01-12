using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Faithlife.Build;

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
                ("PublishWindowsPdb", "false"),
                ("ContinuousIntegrationBuild", "true"),
                ("OfficialBuildId", $"{DateTime.UtcNow.ToString("yyyyMMdd")}.{buildNumber}"),
                ("WindowsSDKBuildToolsBinVersionedFolder", TryGetWindowsSdkBinFolder() ?? ""),
            ];
        }

        return [];
    };
    build.AddDotNetTargets(settings);
});

static string? TryGetWindowsSdkBinFolder()
{
    var nugetRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

    var packageRoot = Path.Combine(nugetRoot, "microsoft.windows.sdk.buildtools");
    if (!Directory.Exists(packageRoot))
        return null;

    var bestPackage = Directory.EnumerateDirectories(packageRoot)
        .Select(dir => Version.TryParse(Path.GetFileName(dir), out var version) ? (Directory: dir, Version: version) : (null, null))
        .Where(x => x.Version is not null)
        .OrderByDescending<(string? Directory, Version? Version), Version>(x => x.Version!)
        .FirstOrDefault();

    if (bestPackage.Directory is null)
        return null;

    var binRoot = Path.Combine(bestPackage.Directory, "bin");
    if (!Directory.Exists(binRoot))
        return null;

    var bestBin = Directory.EnumerateDirectories(binRoot)
        .Select(dir => Version.TryParse(Path.GetFileName(dir), out var version) ? (Directory: dir, Version: version) : (null, null))
        .Where(entry => entry.Directory is not null)
        .OrderByDescending<(string? Directory, Version? Version), Version>(x => x.Version!)
        .FirstOrDefault();

    return bestBin.Directory;
}

