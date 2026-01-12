using Faithlife.Build;

return BuildRunner.Execute(args, build =>
{
    build.AddDotNetTargets();
});

