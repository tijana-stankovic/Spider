namespace PhotoController;

public class Command {
    public string Name { get; }
    public string[] Args { get; }

    public Command(string cmdName, string[] cmdArgs) {
        Name = cmdName;
        Args = cmdArgs;
    }
}
