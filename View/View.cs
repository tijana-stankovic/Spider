namespace SpiderView;

using SpiderStatus;
using System;
using System.Collections.Generic;

public class View {
    static public void FullProgramInfo() {
        string version = "1.0";
        string projectName = "Spider";
        string course = "Searching the Web - NDBI038 - student project";
        string author = "Tijana Stankovic";
        string email = "tijana.stankovic@gmail.com";
        string university = "Charles University, Faculty of Mathematics and Physics";

        Console.WriteLine();
        Console.WriteLine(projectName + " [v " + version + "]");
        Console.WriteLine(course);
        Console.WriteLine("(c) " + author + ", " + email);
        Console.WriteLine(university);
        Console.WriteLine();
    }

    static public void PrintPrompt() {
        string prompt = "> ";
        Console.Write(prompt);
    }

    static public void Print(string line) {
        Console.WriteLine(line);
    }

    static public void Print(string line, bool newLine) {
        if (newLine) {
            Console.WriteLine(line);
        } else {
            Console.Write(line);
        }
    }

    static public void PrintStatus(StatusCode statusCode) {
        Console.WriteLine(StatusMessages.GetStatusMessage(statusCode));
    }

    static public void PrintDBStatistics(Dictionary<string, int> dbStatistics) {
        Print("Current database statistics:");
        int spNamesCount = dbStatistics["NAMES"];
        if (spNamesCount > 0) {
            Print("   - Number of starting point names: " + dbStatistics["NAMES"] + " (use 'LS' command to get a list)");

            int pageCount = dbStatistics["PAGES"];
            if (pageCount > 0) {
                Print("   - Number of websites: " + dbStatistics["WEBSITES"] + " (use 'LW' command to get a list)");
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
}
