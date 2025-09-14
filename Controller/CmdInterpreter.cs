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

            case "PSCAN":
                PScan(cmd.Args);
                break;

            case "SCAN":
                Scan(cmd.Args);
                break;

            case "SCANK":
                ScanKeywords(cmd.Args);
                break;

            case "FIND":
            case "F":
                Find(cmd.Args);
                break;

            case "LOG":
                Log(cmd.Args);
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
        View.Print("  Exit the program.");
        View.Print("  If there are unsaved changes, the program will display a control question.");
        View.Print("- SAVE [<db-filename>]");
        View.Print("  Saving the current memory state to a local file.");
        View.Print("  Default name for this file: spider_db.sdb");
        View.Print("  New filename can be specified as parameter.");
        View.Print("  The name of the file can also be specified as a parameter when starting the program.");
        View.Print("- ADD (A)");
        View.Print("    - ADD <name> <web-page-url> <internal-depth> <external-depth> [<base-url>]");
        View.Print("      Adds a new starting point to the database or updates an existing one.");
        View.Print("    - ADD KEYWORD <keyword>");
        View.Print("      ADD KEY <keyword>");
        View.Print("      Adds a new keyword to the database or updates an existing one.");
        View.Print("- AK");
        View.Print("  Short form for ADD KEYWORD (ADD KEY) command. For details, see ADD command.");
        View.Print("- REMOVE (R)");
        View.Print("    - REMOVE <name>");
        View.Print("      Removes a starting point with the specified name from the database.");
        View.Print("    - REMOVE KEYWORD <keyword>");
        View.Print("      REMOVE KEY <keyword>");
        View.Print("      Removes an existing keyword from the database.");
        View.Print("- RK");
        View.Print("  Short form for REMOVE KEYWORD (REMOVE KEY) command. For details, see REMOVE command.");
        View.Print("- LIST (L)");
        View.Print("    - LIST KEYWORDS (LIST KEYS)");
        View.Print("      Lists all existing keywords in the database.");
        View.Print("    - LIST NAMES (LIST STARTINGPOINTS)");
        View.Print("      Lists all existing starting points in the database.");
        View.Print("    - LIST");
        View.Print("      Displays database statistics.");
        View.Print("- LK");
        View.Print("  Short form for LIST KEYWORDS command. For details, see LIST command.");
        View.Print("- LN (LS)");
        View.Print("  Short form for LIST NAMES (LIST STARTINGPOINTS) command. For details, see LIST command.");
        View.Print("- SCAN");
        View.Print("    - SCAN [<name-list>] [<keyword-list>]");
        View.Print("      Start web crawling for the specified names and keywords.");
        View.Print("      When specifying multiple names or keywords, the list of values should be enclosed");
        View.Print("      in quotation marks, with values separated by commas.");
        View.Print("      If <name-list> (<keyword-list>) is not specified, all names (keywords) will be used.");
        View.Print("    - SCAN KEYWORDS [<keyword-list>] [<name-list>]");
        View.Print("      SCAN KEYS [<keyword-list>] [<name-list>]");
        View.Print("      Same as the SCAN command, but we specify keywords first, then names.");
        View.Print("- SCANK");
        View.Print("  Short form for SCAN KEYWORDS (SCAN KEYS) command. For details, see SCAN command.");
        View.Print("- FIND (F)");
        View.Print("  FIND <keyword>");
        View.Print("  Find and list URL of all pages in the database that contain the specified keyword.");
        View.Print("- LOG");
        View.Print("  - LOG <new-log-filename>");
        View.Print("    Change the log file name. Default log file name is 'spider_log.txt'.");
        View.Print("  - LOG CLEAR");
        View.Print("    Clear the log file.");
        View.Print("  - LOG");
        View.Print("    Displays the current log settings.");
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

        if (args.Length < 4 || args.Length > 5) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        string name = args[0];

        string url = args[1];
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) {
            url = "https://" + url;
        }

        if (!int.TryParse(args[2], out int internalDepth) || internalDepth < 0) {
            View.Print("ERROR: Internal depth must be a non-negative integer.");
            return;
        }
        if (!int.TryParse(args[3], out int externalDepth) || externalDepth < 0) {
            View.Print("ERROR: External depth must be a non-negative integer.");
            return;
        }
        string baseUrl = WebCrawler.GetBaseDomain(url); // default base URL value
        if (args.Length == 5) { // if base URL is specified as parameter
            baseUrl = args[4];
        }
        if (!baseUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)) {
            var prefix = new Uri(url).Scheme;
            baseUrl = $"{prefix}://{baseUrl}";
        }

        StartingPoint sp = new(name, url, internalDepth, externalDepth, baseUrl);

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
        if (Db.RemoveKeyword(keyword)) {
            View.Print($"Keyword '{keyword}' has been removed from the database!");
        } else {
            View.Print($"Keyword '{keyword}' was not found in the database!");
        }
    }

    private void List(string[] args) {
        if (args.Length == 0) {
            View.PrintDBStatistics(Db.GetDBStatistics());
            return;
        } 

        if (args.Length == 1 && (args[0].ToUpper() == "KEYWORDS" || args[0].ToUpper() == "KEYS")) {
            ListKeywords(args[1..]);
            return;
        }

        if (args.Length == 1 && (args[0].ToUpper() == "NAMES" || args[0].ToUpper() == "STARTINGPOINTS")) {
            ListStartingPoints(args[1..]);
            return;
        }

        StatusCode = StatusCode.InvalidNumberOfArguments;
        View.PrintStatus(StatusCode);
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
                View.Print($"   {spName} -> {sp.URL} [{sp.InternalDepth}/{sp.ExternalDepth}] {sp.BaseURL}");
            } else {
                View.Print($"   {spName} -> (not found)");
            }
        }
    }

    private void Scan(string[] args) {
        if (args.Length >= 1 && (args[0].ToUpper() == "KEYWORDS" || args[0].ToUpper() == "KEYS")) {
            ScanKeywords(args[1..]);
            return;
        }

        if (args.Length > 2) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        List<string>? names = null;
        List<string>? keywords = null;
        bool stop = false;
        if (args.Length > 0) {
            names = CreateListOfStrings(args[0], 'N', out stop); // create list of names from the first argument
        }
        if (!stop && args.Length > 1) {
            keywords = CreateListOfStrings(args[1], 'K', out stop); // create list of keywords from the second argument
        }

        if (!stop) {
            Scan(names, keywords);
        } else {
            View.Print("Scanning aborted.");
        }
    }

    // create list of strings from a comma-separated string of values
    // removes duplicates and values not found in the database
    // if result is empty list, return null
    private List<string>? CreateListOfStrings(string input, char namesOrKeywords, out bool stop) {
        List<string> result = new List<string>();
        stop = false;

        if (string.IsNullOrWhiteSpace(input)) {
            return null;
        }

        // convert input string to list of values (remove duplicates)
        result = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) // split input string on commas
                      .Select(s => s.Trim()) // remove leading and trailing whitespace
                      .Where(s => !string.IsNullOrWhiteSpace(s)) // remove empty values
                      .GroupBy(s => s.ToUpper()) // group by uppercase version
                      .Select(g => g.First()) // select first item from each group (keep original casing)
                      .ToList(); // convert to list

        List<string> allSpecifiedItems = new List<string>(result); // remember all distinct specified items

        // removes all specified items that do not exist in GetKeywords() or GetSPNames()
        List<string> existingItems;
        if (namesOrKeywords == 'K') {
            existingItems = Db.GetKeywords();
        } else if (namesOrKeywords == 'N') {
            existingItems = Db.GetSPNames();
        } else { // it should not reach here
            existingItems = [];
        }
        existingItems = existingItems.Select(s => s.ToUpper()).ToList();

        result.RemoveAll(item => !existingItems.Contains(item.ToUpper())); // this is a final result

        // if some items were removed
        if (allSpecifiedItems.Count != result.Count) {
            // find list of removed items
            List<string> removedItems = allSpecifiedItems.Except(result, StringComparer.OrdinalIgnoreCase).ToList();

            View.Print($"Warning ! Some of the specified { (namesOrKeywords == 'K' ? "keywords" : "starting points")} are not found in the database!");
            if (namesOrKeywords == 'K') {
                View.Print($"   Unknown keywords: {string.Join(", ", removedItems)}");
                char response = CLI.AskYesNo("   Do you want to add them automatically?", false); // no CANCEL option
                switch (response) {
                    case 'Y':
                        foreach (var keyword in removedItems) {
                            Db.AddKeyword(keyword);
                            result.Add(keyword);
                        }
                        break;

                    case 'N':
                        stop = true;
                        break;
                }

            } else if (namesOrKeywords == 'N') {
                View.Print($"   Undefined starting points: {string.Join(", ", removedItems)}");
                View.Print("   Please define them before using.");
                stop = true;
            }
        }

        if (result.Count == 0) {
            return null;
        }

        return result;
    }

    private void ScanKeywords(string[] args) {
        if (args.Length > 2) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        if (args.Length > 2) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        List<string>? names = null;
        List<string>? keywords = null;
        bool stop = false;
        if (args.Length > 0) {
            keywords = CreateListOfStrings(args[0], 'K', out stop); // create list of keywords from the first argument
        }
        if (!stop && args.Length > 1) {
            names = CreateListOfStrings(args[1], 'N', out stop); // create list of names from the second argument
        }

        if (!stop) {
            Scan(names, keywords);
        } else {
            View.Print("Scanning aborted.");
        }
    }

    // customNames, customKeywords = user specified set of names and/or keywords
    private void Scan(List<string>? customNames, List<string>? customKeywords) {
        // if parallel crawling is already active, print warning and exit immediately
        // TODO: lock
        if (PWebCrawler.PScanActive) {
            View.Print("Warning: Parallel crawling is already in progress.");
            View.Print("         Please wait for it to complete...");

            // TODO: lock
            if (PWebCrawler.ActiveThreads > 0) {
                View.Print($"         Currently active crawling threads: {PWebCrawler.ActiveThreads}");
            }

            return;
        }

        View.LogOpen();

        View.LogPrint(View.FullLine, true);
        View.LogPrint("Terms used during the scanning process:", true);
        if (customNames == null) {
            View.LogPrint($"   Names: (ALL)", true);
        } else {
            View.LogPrint($"   Names: {string.Join(", ", customNames)}", true);
        }
        if (customKeywords == null) {
            View.LogPrint($"   Keywords: (ALL)", true);
        } else {
            View.LogPrint($"   Keywords: {string.Join(", ", customKeywords)}", true);
        }
        View.LogPrint(View.FullLine, true);

        List<string> names;
        if (customNames == null) {
            names = Db.GetSPNames();
        } else {
            names = customNames;
        }
        List<StartingPoint> startingPoints = [];
        foreach (string spName in names) {
            StartingPoint? sp = Db.GetStartingPoint(spName);
            if (sp != null) {
                startingPoints.Add(sp);
            }
        }

        List<string> keywords;
        if (customKeywords == null) {
            keywords = Db.GetKeywords();
        } else {
            keywords = customKeywords;
        }

        // removes all existing page/keywords connection for specified starting points and keywords
        // (new page/keywords connections will be added later)
        var count = names.Count * keywords.Count;
        if (count > 0) {
            foreach (string spName in names) {
                foreach (string keyword in keywords) {
                    var pageIDs = GetCommonPageIDs(spName, keyword);
                    foreach (var pageID in pageIDs) {
                        Db.RemovePageKeywordConnection(keyword, pageID);
                    }
                }
            }
            View.LogPrint("", true);
            View.LogPrint("Clearing the obsolete page-keywords connections from the database... completed successfully.", true);
        }

        // Start crawling
        if (PWebCrawler.NumOfThreads > 1) { // parallel crawling
            View.LogPrint("", true);
            View.LogPrint("The parallel web crawling process has started in the background.", true);
            View.LogPrint("", true);

            Task.Run(() => {
                // TODO: lock
                PWebCrawler.PScanActive = true;
                CrawlResult? result = null;
                try {
                    result = PWebCrawler.Crawl(startingPoints, keywords);
                    View.LogPrint("The parallel crawling completed successfully.", false); // print only to log !
                }
                catch (Exception _) {
                    // TODO: log error
                }

                if (result != null) {
                    // Process the result
                    View.LogPrint($"Number of pages visited: {result.VisitedUrls.Count}", true); // TODO: print only to log !

                    View.LogPrint("", true); // TODO: print only to log !
                    View.LogPrint(View.FullLine, true); // TODO: print only to log !

                    // create new page/keywords connections in the database based on result.UrlToKeywords
                    var pageCount = result.UrlToKeywords.Count;
                    if (pageCount > 0) {
                        foreach (var url in result.UrlToKeywords) { // for all found URLs
                            var (keywordsSet, spName) = url.Value; // extract keywords and starting point name
                                                                // create new page
                            DBPage page = new() {
                                Name = spName,
                                URL = url.Key,
                                Keywords = keywordsSet
                            };

                            Db.AddPage(page); // add page to the database
                        }
                        View.LogPrint($"Creating new page-keywords connections in the database: {pageCount} ... completed successfully.", true); // TODO: print only to log !
                    } else {
                        View.LogPrint("No page-keywords connections found.", true); // TODO: print only to log !
                    }
                    View.LogPrint(View.FullLine, true); // TODO: print only to log !

                    if (result.UrlToKeywords.Count > 0) {
                        View.LogPrint();
                        View.LogPrint("Crawling results:");
                        foreach (var url in result.UrlToKeywords) {
                            View.LogPrint($"URL: {url.Key}");
                            View.LogPrint($"    Starting point: {url.Value.spName}");
                            View.LogPrint($"    Found keywords: {string.Join(", ", url.Value.keywordsSet)}");
                        }
                        View.LogPrint();
                        foreach (var keyword in result.KeywordToUrls) {
                            View.LogPrint($"Keyword: {keyword.Key}");
                            foreach (var matchUrl in keyword.Value.urlSet) {
                                View.LogPrint($"    -> {matchUrl}");
                            }
                        }
                        View.LogPrint();
                        View.LogPrint(View.FullLine);
                    }
                }

                View.LogClose();
                // TODO: lock
                PWebCrawler.PScanActive = false;
            });
        } else { // sequential crawling
            View.LogPrint("", true);
            View.LogPrint("The web crawling process has started.", true);
            View.LogPrint("", true);

            CrawlResult result = WebCrawler.Crawl(startingPoints, keywords).GetAwaiter().GetResult();

            View.LogPrint("", true);
            View.LogPrint("The web crawling process has finished.", true);
            View.LogPrint($"Number of pages visited: {result.VisitedUrls.Count}", true);

            View.LogPrint("", true);
            View.LogPrint(View.FullLine, true);

            // create new page/keywords connections in the database based on result.UrlToKeywords
            var pageCount = result.UrlToKeywords.Count;
            if (pageCount > 0) {
                foreach (var url in result.UrlToKeywords) { // for all found URLs
                    var (keywordsSet, spName) = url.Value; // extract keywords and starting point name
                                                        // create new page
                    DBPage page = new() {
                        Name = spName,
                        URL = url.Key,
                        Keywords = keywordsSet
                    };

                    Db.AddPage(page); // add page to the database
                }
                View.LogPrint($"Creating new page-keywords connections in the database: {pageCount} ... completed successfully.", true);
            } else {
                View.LogPrint("No page-keywords connections found.", true);
            }
            View.LogPrint(View.FullLine, true);

            if (result.UrlToKeywords.Count > 0) {
                View.LogPrint();
                View.LogPrint("Crawling results:");
                foreach (var url in result.UrlToKeywords) {
                    View.LogPrint($"URL: {url.Key}");
                    View.LogPrint($"    Starting point: {url.Value.spName}");
                    View.LogPrint($"    Found keywords: {string.Join(", ", url.Value.keywordsSet)}");
                }
                View.LogPrint();
                foreach (var keyword in result.KeywordToUrls) {
                    View.LogPrint($"Keyword: {keyword.Key}");
                    foreach (var matchUrl in keyword.Value.urlSet) {
                        View.LogPrint($"    -> {matchUrl}");
                    }
                }
                View.LogPrint();
                View.LogPrint(View.FullLine);
            }

            View.LogClose();
        }
    }

    // returns the set of common page IDs for a given name and keyword
    public HashSet<int> GetCommonPageIDs(string name, string keyword) {
        var namePages = Db.GetPageIDsWithName(name);
        var keywordPages = Db.GetPageIDsWithKeyword(keyword);

        if (namePages == null || keywordPages == null) {
            return [];
        }

        var intersection = new HashSet<int>(namePages);
        intersection.IntersectWith(keywordPages);

        return intersection;
    }

    private void Find(string[] args) {
        if (args.Length != 1) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        var keyword = args[0];
        if (!Db.GetKeywords().Contains(keyword.ToUpper(), StringComparer.OrdinalIgnoreCase)) {
            View.Print($"The keyword '{keyword}' does not exist.");
            return;
        }

        var pageIDs = Db.GetPageIDsWithKeyword(keyword);

        if (pageIDs != null) {
            List<string> pageUrls = [];

            foreach (var pageID in pageIDs) {
                var page = Db.GetPage(pageID);
                if (page != null) {
                    pageUrls.Add(page.URL);
                }
            }

            pageUrls.Sort();

            View.Print($"List of pages with the keyword '{keyword}'... ");
            foreach (var url in pageUrls) {
                View.Print($"   {url}");
            }
        } else {
            View.Print($"No pages found with the keyword '{keyword}'.");
        }
    }

    private void Log(string[] args)
    {
        if (args.Length == 0) {
            View.LogPrintCurrentStatus();
            return;
        }

        string command = args[0].ToUpper();

        if ((command != "FILE" && args.Length != 1) ||
            (command == "FILE" && args.Length != 2)) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        if (command == "FILE") {
            View.LogFileName = args[1];
            Db.SaveLogParameters();
            View.Print($"Log file name set to {args[1]}");
        } else if (command == "CLEAR") {
            View.LogClear();
            View.Print("Log cleared.");
        } else {
            StatusCode = StatusCode.UnknownLogCommand;
            View.PrintStatus(StatusCode);
        }
    }

    private void PScan(string[] args) {
        if (args.Length > 1) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        if (args.Length == 0) { // no arguments, print current status
            PrintPScanStatus();
        } else { // argument provided, set number of threads
            bool invalidArgument = false;
            if (int.TryParse(args[0], out int numOfThreads)) {
                if (numOfThreads < 1 || numOfThreads > PWebCrawler.MaxNumOfThreads) {
                    invalidArgument = true;
                } else {
                    PWebCrawler.NumOfThreads = numOfThreads;
                    PrintPScanStatus();
                }
            } else {
                invalidArgument = true;
            }
            if (invalidArgument) {
                StatusCode = StatusCode.InvalidArgument;
                View.PrintStatus(StatusCode);
                View.Print("Valid range is 1 to " + PWebCrawler.MaxNumOfThreads);
            }
        }
    }

    static void PrintPScanStatus() {
        if (PWebCrawler.NumOfThreads > 1) {
            View.Print("Parallel web crawling is enabled. Number of threads: " + PWebCrawler.NumOfThreads);
        } else {
            View.Print("Parallel web crawling is disabled.");
        }
    }
}
