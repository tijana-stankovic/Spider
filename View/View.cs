namespace PhotoView;

using PhotoStatus;
using System;
using System.Collections.Generic;

public class View {
    public void FullProgramInfo() {
        string version = "1.0";
        string projectName = "Photo Organizer";
        string course = "Programming in Java Language - NPRG013 - student project";
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

    public void PrintPrompt() {
        string prompt = "> ";
        Console.Write(prompt);
    }

    public void Print(string line) {
        Console.WriteLine(line);
    }

    public void Print(string line, bool newLine) {
        if (newLine) {
            Console.WriteLine(line);
        } else {
            Console.Write(line);
        }
    }

    public void PrintStatus(StatusCode statusCode) {
        Console.WriteLine(StatusMessages.GetStatusMessage(statusCode));
    }

    public void PrintDBStatistics(Dictionary<string, int> dbStatistics) {
        Print("Current database statistics:");
        int fileCount = dbStatistics["FILES"];
        if (fileCount > 0) {
            Print("   - Number of files: " + fileCount);
            Print("   - Number of directories: " + dbStatistics["DIRS"] + " (use 'LD' command to get a list)");
            Print("   - Number of keywords: " + dbStatistics["KEYS"] + " (use 'LK' command to get a list)");
        } else {
            Print("   - Number of files: 0 (use 'ADD' command to add files to the database)");
        }
    }
}
