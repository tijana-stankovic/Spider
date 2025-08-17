namespace SpiderController;

using SpiderView;
using System;
using System.Collections.Generic;
using System.IO;

public class CLI {
    static public Command ReadCommand() {
        List<string> argList = [];

        try {
            string? line = Console.ReadLine();
            if (line != null) {
                var currentWord = new System.Text.StringBuilder();
                bool insideQuotes = false;

                foreach (char c in line) {
                    if (c == '"') {
                        if (!insideQuotes) {
                            insideQuotes = true;
                        } else {
                            insideQuotes = false;
                            // closing quote, add the quoted parameter
                            argList.Add(currentWord.ToString());
                            currentWord.Clear();
                        }
                    } else if (Char.IsWhiteSpace(c) && !insideQuotes) {
                        if (currentWord.Length > 0) {
                            argList.Add(currentWord.ToString());
                            currentWord.Clear();
                        }
                    } else {
                        currentWord.Append(c);
                    }
                }

                // add the last word
                if (currentWord.Length > 0) {
                    argList.Add(currentWord.ToString());
                }
            }
        } catch (IOException) {
            Console.Error.WriteLine("IOException occurred");
        }

        string cmd;
        string[] cmdArgs;
        if (argList.Count > 0) {
            cmd = argList[0];
            cmdArgs = new string[argList.Count - 1];
            for (int i = 1; i < argList.Count; i++) {
                cmdArgs[i - 1] = argList[i];
            }
        } else {
            cmd = "";
            cmdArgs = [];
        }

        Command command = new(cmd, cmdArgs);
        return command;
    }

    static public char AskYesNo(string message, bool cancel) {
        string prompt = cancel ? " (Yes/No/Cancel)" : " (Yes/No)";
        string? response;

        while (true) {
            View.Print(message + prompt + ": ", false);
            try {
                response = Console.ReadLine()?.Trim().ToLower();
            } catch (IOException) {
                Console.Error.WriteLine("IOException occurred while reading input");
                continue;
            }

            if (response == "y" || response == "yes") {
                return 'Y';
            } else if (response == "n" || response == "no") {
                return 'N';
            } else if (cancel && (response == "c" || response == "cancel")) {
                return 'C';
            } else {
                View.Print("Invalid response. Please enter 'Yes', 'No'" + (cancel ? ", or 'Cancel'" : "") + ".");
            }
        }
    }
}
