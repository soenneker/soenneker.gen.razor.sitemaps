namespace Soenneker.Gen.Razor.Sitemaps.BuildTasks;

public sealed class BuildTasksCommandLineArgs
{
    public string[] Args { get; }

    public BuildTasksCommandLineArgs(string[] args)
    {
        Args = args;
    }
}
