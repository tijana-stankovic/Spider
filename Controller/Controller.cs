namespace SpiderController;

using SpiderStatus;
using SpiderDB;
using SpiderView;

/// <summary>
/// The top-level class of the Controller.
/// It initializes the other application and Controller parts and executes the main application loop.
/// </summary>
public class Controller {
    private DB Db { get; set; }
    private CmdInterpreter Interpreter { get; set; }

    public Controller(string[] args) {
        View.FullProgramInfo();

        string dbFilename = GetFilename(args);
        Db = new DB(dbFilename);
        switch (Db.StatusCode) {
            case StatusCode.NoError: // do nothing
                break;

            case StatusCode.DbFileDoesNotExist:
            case StatusCode.DbFileDoesNotExistError:
            case StatusCode.DbFileIncompatibleFormat:
            case StatusCode.DbFileReadError:
                View.PrintStatus(Db.StatusCode);
                break;

            case StatusCode.DbFileWriteError:
                View.PrintStatus(Db.StatusCode);
                break;

            default:
                View.PrintStatus(StatusCode.UnexpectedStatus);
                break;
        }

        View.Print();
        View.LogPrintCurrentStatus();
        View.Print();
        View.PrintDBStatistics(Db.GetDBStatistics());

        Interpreter = new CmdInterpreter(Db);
    }

    static private string GetFilename(string[] args) {
        string fileName;

        if (args.Length == 0) {
            fileName = DB.DefaultDbFilename;
            View.Print("The default database file name will be used: " + fileName);
        } else {
            fileName = args[0];
            if (!fileName.Contains('.')) {
                fileName += "." + DB.DefaultDbExtension;
            }
            View.Print("The database file name: " + fileName);
        }

        return fileName;
    }

    /// <summary>
    /// *** MAIN APPLICATION LOOP ***
    /// Runs the main application loop.
    /// Processes user commands until the "quit signal" (END command) is received.
    /// </summary>
    public void Run() {
        View.Print();

        bool quit = false;
        while (!quit) {
            View.PrintPrompt();
            Command cmd = CLI.ReadCommand();
            Interpreter.ExecuteCommand(cmd);
            quit = Interpreter.QuitSignal;
        }
    }
}
