namespace SpiderController;

using SpiderDB;
using SpiderStatus;
using SpiderView;
using SpiderHttp;

public class CmdInterpreter(DB db) {
    private DB Db { get; set; } = db;
    public StatusCode StatusCode { get; set; } = StatusCode.NoError;
    public bool QuitSignal { get; set; } = false;

    public void ExecuteCommand(Command cmd) {
        StatusCode = StatusCode.NoError;

        string command = cmd.Name.ToUpper();

        switch (command) {
            case "": // do nothing
                break;

            case "H":
            case "HELP":
                Help();
                break;

            case "AB":
            case "ABOUT":
                About();
                break;

            case "E":
            case "X":
            case "EXIT":
                Exit();
                break;

            case "SAVE":
                Save(cmd.Args);
                break;

            case "A":
            case "ADD":
                Add(cmd.Args);
                break;

            case "AK":
                AddKeyword(cmd.Args);
                break;

            case "R":
            case "REMOVE":
                Remove(cmd.Args);
                break;

            case "RK":
                RemoveKeyword(cmd.Args);
                break;

            case "L":
            case "LIST":
                List(cmd.Args);
                break;

            case "LK":
                ListKeywords(cmd.Args);
                break;

            case "LN":
            case "LS":
                ListStartingPoints(cmd.Args);
                break;

            case "LW":
                ListWebsites(cmd.Args);
                break;

            case "SCAN":
                Scan(cmd.Args);
                break;

            default:
                StatusCode = StatusCode.UnknownCommand;
                View.PrintStatus(StatusCode);
                break;
        }
    }

    static private void Help() {
        View.Print("List of available commands:");
        View.Print("- HELP (H)");
        View.Print("  Display page with list of commands.");
        View.Print("- ABOUT (AB)");
        View.Print("  Display information about program.");
        View.Print("- EXIT (E, X)");
        View.Print("  Exiting the program.");
        View.Print("  If there are unsaved changes, the program will display a control question.");
        View.Print("- SAVE [<db-filename>]");
        View.Print("  Saving the current memory state to a local file.");
        View.Print("  Default name for this file: photo_db.pdb");
        View.Print("  New filename can be specified as parameter.");
        View.Print("  The name of the file can also be specified as a parameter when starting the program.");
        View.Print("- ADD (A)");
        View.Print("    - ADD KEYWORD <keyword>");
        View.Print("- AK");
        View.Print("  Short form for ADD KEYWORD command. For details, see ADD command.");
        View.Print("- REMOVE (R)");
        View.Print("    - REMOVE KEYWORD <keyword>");
        View.Print("- RK");
        View.Print("  Short form for REMOVE KEYWORD command. For details, see REMOVE command.");
        View.Print("- LIST (L)");
        View.Print("    - LIST KEYWORDS (LIST KEYS)");
        View.Print("      Lists all existing keywords in the database.");
        View.Print("- LK");
        View.Print("  Short form for LIST KEYWORDS command. For details, see LIST command.");
    }

    static private void About() {
        View.FullProgramInfo();
    }

    private void Exit() {
        if (Db.IsChanged()) {
            char response = CLI.AskYesNo("There are unsaved changes. Do you want to save them?", true);
            switch (response) {
                case 'Y':
                    Save();
                    if (Db.StatusCode == StatusCode.NoError) {
                        QuitSignal = true;
                    }
                    break;

                case 'N':
                    QuitSignal = true;
                    break;

                case 'C':
                default:
                    break;
            }
        } else {
            QuitSignal = true;
        }
    }

    private void Save(string[] args) {
        if (args.Length > 1) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        if (args.Length == 1) {
            string newDbFilename = args[0];
            if (Db.DbFilename != newDbFilename) {
                Db.DbFilename = newDbFilename;
            }
        }

        Save();
    }

    private void Save() {
        if (Db.IsChanged()) {
            Db.WriteDB();

            switch (Db.StatusCode) {
                case StatusCode.NoError:
                    View.Print($"Changes saved successfully (DB filename: '{Db.DbFilename}').");
                    break;

                case StatusCode.DbFileWriteError:
                    View.PrintStatus(Db.StatusCode);
                    break;

                default:
                    View.PrintStatus(StatusCode.UnexpectedStatus);
                    break;
            }
        } else {
            View.Print("There are no changes to save.");
        }
    }

    private void Add(string[] args) {
        if (args.Length >= 1 && (args[0].ToUpper() == "KEYWORD" || args[0].ToUpper() == "KEY")) {
            AddKeyword(args[1..]);
            return;
        }

        if (args.Length != 4) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        string name = args[0];
        string url = args[1];
        if (!int.TryParse(args[2], out int internalDepth) || internalDepth < 0) {
            View.Print("ERROR: Internal depth must be a non-negative integer.");
            return;
        }
        if (!int.TryParse(args[3], out int externalDepth) || externalDepth < 0) {
            View.Print("ERROR: External depth must be a non-negative integer.");
            return;
        }

        StartingPoint sp = new(name, url, internalDepth, externalDepth);

        if (Db.AddStartingPoint(sp)) {
            View.Print($"Starting point '{name}' has been added to the database.");
        } else {
            View.Print($"Starting point '{name}' has been updated.");
        }
    }

    private void AddKeyword(string[] args) {
        if (args.Length != 1) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        string keyword = args[0];
        string? oldKeyword = Db.AddKeyword(keyword);

        if (oldKeyword != null) {
            if (oldKeyword != keyword) {
                View.Print($"Keyword '{keyword}' has been updated in the database! Old value: '{oldKeyword}'");
            } else {
                View.Print($"Keyword '{keyword}' already exists in the database.");
            }
        } else {
            View.Print($"Keyword '{keyword}' has been added to the database!");
        }
    }

    private void Remove(string[] args) {
        if (args.Length >= 1 && (args[0].ToUpper() == "KEYWORD" || args[0].ToUpper() == "KEY")) {
            RemoveKeyword(args[1..]);
            return;
        }

        if (args.Length != 1) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        string name = args[0];
        if (Db.RemoveStartingPoint(name)) {
            View.Print($"Starting point '{name}' has been removed from the database.");
        } else {
            View.Print($"Starting point '{name}' was not found in the database!");
        }
    }

    private void RemoveKeyword(string[] args) {
        if (args.Length != 1) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        string keyword = args[0];
        string? oldKeyword = Db.RemoveKeyword(keyword);

        if (oldKeyword != null) {
            View.Print($"Keyword '{keyword}' has been removed from the database!");
        } else {
            View.Print($"Keyword '{keyword}' was not found in the database!");
        }
    }

    private void List(string[] args) {
        if (args.Length >= 1 && (args[0].ToUpper() == "KEYWORDS" || args[0].ToUpper() == "KEYS")) {
            ListKeywords(args[1..]);
            return;
        }

        if (args.Length >= 1 && (args[0].ToUpper() == "NAMES" || args[0].ToUpper() == "STARTINGPOINTS")) {
            ListStartingPoints(args[1..]);
            return;
        }

        if (args.Length >= 1 && (args[0].ToUpper() == "WEBSITES" || args[0].ToUpper() == "WEBS")) {
            ListWebsites(args[1..]);
            return;
        }

        List(args, false);
    }

    private void List(string[] args, bool allDetails) {
        if (args.Length == 0) {
            View.PrintDBStatistics(Db.GetDBStatistics());
            return;
        } else if (args.Length > 0) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }
    }

    private void ListKeywords(string[] args) {
        if (args.Length > 0) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        View.Print("List of keywords in the database:");
        foreach (string keyword in Db.GetKeywords()) {
            View.Print($"   {keyword}");
        }
    }

    private void ListStartingPoints(string[] args) {
        if (args.Length > 0) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        View.Print("List of names (starting points) in the database:");
        foreach (string spName in Db.GetSPNames()) {
            StartingPoint? sp = Db.GetStartingPoint(spName);
            if (sp != null) {
                View.Print($"   {spName} -> {sp.URL}");
            } else {
                View.Print($"   {spName} -> (not found)");
            }
        }
    }

    private void ListWebsites(string[] args) {
        if (args.Length > 0) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        View.Print("List of websites in the database:");
        foreach (string website in Db.GetFoundWebsites()) {
            View.Print($"   {website}");
        }
    }

    private void Scan(string[] args) {
        View.Print("The web crawling process has started.");

        List<StartingPoint> startingPoints = [];
        foreach (string spName in Db.GetSPNames())
        {
            StartingPoint? sp = Db.GetStartingPoint(spName);
            if (sp != null) {
                startingPoints.Add(sp);
            }
        }

        var keywords = Db.GetKeywords();

        // CrawlResult result = await WebCrawler.CrawlAsync(startingPoints, keywords);
        CrawlResult result = WebCrawler.CrawlAsync(startingPoints, keywords).GetAwaiter().GetResult();

        View.Print("The web crawling process has finished.");
        View.Print("");
        View.Print("Crawling results:");
        foreach (var kv in result.KeywordToUrls) {
            Console.WriteLine($"Keyword: {kv.Key}");
            foreach (var matchUrl in kv.Value) {
                Console.WriteLine($"  -> {matchUrl}");
            }
        }        
    }
}
