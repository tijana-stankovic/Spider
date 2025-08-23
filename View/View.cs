namespace SpiderView;

using SpiderStatus;
using SpiderDB;
using System;
using System.Collections.Generic;

public class View {
    public static string LogFileName { get; set; } = "spider_log.txt";
    private static StreamWriter? _logWriter = null;

    public static string FullLine { get; } = "--------------------------------------------------------------------------------";
    public static string FullDoubleLine { get; } = "================================================================================";

    static public void Print(string line = "") {
        Console.WriteLine(line);
    }

    static public void Print(string line, bool newLine) {
        if (newLine) {
            Console.WriteLine(line);
        } else {
            Console.Write(line);
        }
    }

    static public void FullProgramInfo() {
        string version = "1.0";
        string projectName = "Spider";
        string course = "Searching the Web - NDBI038 - student project";
        string author = "Tijana Stankovic";
        string email = "tijana.stankovic@gmail.com";
        string university = "Charles University, Faculty of Mathematics and Physics";

        Print();
        Print(projectName + " [v " + version + "]");
        Print(course);
        Print("(c) " + author + ", " + email);
        Print(university);
        Print();
    }

    static public void PrintPrompt() {
        string prompt = "> ";
        Print(prompt, false);
    }

    static public void PrintStatus(StatusCode statusCode) {
        Print(StatusMessages.GetStatusMessage(statusCode));
    }

    static public void PrintDBStatistics(Dictionary<string, int> dbStatistics) {
        Print("Current database statistics:");
        int spNamesCount = dbStatistics["NAMES"];
        if (spNamesCount > 0) {
            Print("   - Number of starting point names: " + dbStatistics["NAMES"] + " (use 'LS' command to get a list)");

            int pageCount = dbStatistics["PAGES"];
            if (pageCount > 0) {
                Print("   - Number of HTML pages: " + pageCount);
            } else {
                Print("   - Number of HTML pages: 0 (use 'SCAN' command to initiates the web crawling process)");
            }
        } else {
            Print("   - Number of starting point names: 0 (use 'ADD' command to add starting points to the database)");
        }
        int keywordCount = dbStatistics["KEYS"];
        if (keywordCount > 0) {
            Print("   - Number of keywords: " + keywordCount + " (use 'LK' command to get a list)");
        } else {
            Print("   - Number of keywords: 0 (use 'AK' command to add keywords to the database)");
        }
    }

    public static void LogOpen() {
        if (_logWriter == null) {
            _logWriter = new StreamWriter(LogFileName, append: true);
            LogPrint(FullDoubleLine);
            LogPrint("Log opened at " + DateTime.Now);
        }
    }

    public static void LogClose() {
        LogPrint("Log closed at " + DateTime.Now);
        LogPrint(FullDoubleLine);
        _logWriter?.Close();
        _logWriter = null;
    }

    public static void LogClear() {
        bool logWasOpen = _logWriter != null;

        if (logWasOpen) {
            LogClose();
        }

        File.WriteAllText(LogFileName, string.Empty); // clear the file

        if (logWasOpen) {
            LogOpen();
        }
    }

    public static void LogPrint(string message = "", bool printToConsole = false) { 
        if (printToConsole) {
            View.Print(message);
        }
        _logWriter?.WriteLine(message);
    }

    public static void LogPrintCurrentStatus() { 
        Print($"Log filename: {LogFileName}");
    }
}
