namespace SpiderController;

public class Command(string cmdName, string[] cmdArgs)
{
    public string Name { get; } = cmdName;
    public string[] Args { get; } = cmdArgs;
}
