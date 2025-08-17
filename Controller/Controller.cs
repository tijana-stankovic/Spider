namespace PhotoController;

using PhotoStatus;
using PhotoDB;
using PhotoView;

public class Controller {
    private View View { get; set; }
    private DB Db { get; set; }
    private CmdInterpreter Interpreter { get; set; }

    public Controller(string[] args) {
        View = new View();
        View.FullProgramInfo();

        string dbFilename = GetFilename(args);
        Db = new DB(dbFilename);
        switch (Db.StatusCode) {
            case StatusCode.NoError: // do nothing
                break;

            case StatusCode.DbFileDoesNotExist:
            case StatusCode.DbFileIncompatibleFormat:
            case StatusCode.DbFileReadError:
                View.PrintStatus(Db.StatusCode);
                break;

            default:
                View.PrintStatus(StatusCode.UnexpectedStatus);
                break;
        }

        View.Print("");
        View.PrintDBStatistics(Db.GetDBStatistics());

        Interpreter = new CmdInterpreter(Db, View);
    }

    private string GetFilename(string[] args) {
        string fileName;

        if (args.Length == 0) {
            fileName = DB.DefaultDbFilename;
            View.Print("The default name of the DB file will be used: " + fileName);
        } else {
            fileName = args[0];
            if (!fileName.Contains('.')) {
                fileName += ".pdb";
            }
            View.Print("The DB filename: " + fileName);
        }

        return fileName;
    }

    public void Run() {
        View.Print("");

        Interpreter.Cli = new CLI();
        bool quit = false;
        while (!quit) {
            View.PrintPrompt();
            Command cmd = Interpreter.Cli.ReadCommand();
            Interpreter.ExecuteCommand(cmd);
            quit = Interpreter.QuitSignal;
        }
    }
}
