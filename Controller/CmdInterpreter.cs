namespace PhotoController;

using PhotoDB;
using PhotoStatus;
using PhotoUtil;
using PhotoView;

public class CmdInterpreter {
    public CmdInterpreter(DB db, View view) {
        Db = db;
        View = view;
        StatusCode = StatusCode.NoError;
        QuitSignal = false;
        Cli = null;
    }

    private View View { get; set; }
    private DB Db { get; set; }
    public StatusCode StatusCode { get; set; }
    public bool QuitSignal { get; set; }
    public CLI? Cli { get; set; }

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

            case "LD":
            case "LF":
                ListDirectories(cmd.Args);
                break;

            case "D":
            case "DETAILS":
                Details(cmd.Args);
                break;

            case "DUP":
            case "DD":
            case "DUPLICATES":
                Duplicates(cmd.Args);
                break;

            case "S":
            case "SCAN":
                Scan(cmd.Args);
                break;

            default:
                StatusCode = StatusCode.UnknownCommand;
                View.PrintStatus(StatusCode);
                break;
        }
    }

    private void Help() {
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
        View.Print("    - ADD <folder> or <filename>");
        View.Print("      Adds all images from the specified <folder> or");
        View.Print("      only the one specified by <filename> to the in-memory database.");
        View.Print("    - ADD KEYWORD <keyword> <folder> or <filename>");
        View.Print("      All images from the specified folder <folder> or");
        View.Print("      only the one specified by <filename> get the keyword specified by <keyword>.");
        View.Print("- AK");
        View.Print("  Short form for ADD KEYWORD command. For details, see ADD command.");
        View.Print("- REMOVE (R)");
        View.Print("    - REMOVE <folder> or <filename>");
        View.Print("      Removes all images from the specified <folder> (including the folder) or");
        View.Print("      only the one specified by <filename> from the in-memory database.");
        View.Print("    - REMOVE KEYWORD <keyword> <folder> or <filename>");
        View.Print("      only the one specified by <filename> will have the specified <keyword> removed from them.");
        View.Print("- RK");
        View.Print("  Short form for REMOVE KEYWORD command. For details, see REMOVE command.");
        View.Print("- LIST (L)");
        View.Print("    - LIST <keyword> or <folder> or <file>");
        View.Print("      Lists all images that have the specified keyword or belong to the specified folder.");
        View.Print("    - LIST KEYWORDS (LIST KEYS)");
        View.Print("      Lists all existing keywords in the database.");
        View.Print("    - LIST DIRECTORIES (LIST DIRS, LIST FOLDERS)");
        View.Print("      Lists all existing directories (folders) in the database.");
        View.Print("    - LIST");
        View.Print("      Displays database statistics.");
        View.Print("- LK");
        View.Print("  Short form for LIST KEYWORDS command. For details, see LIST command.");
        View.Print("- LD (LF)");
        View.Print("  Short form for LIST DIRECTORIES command. For details, see LIST command.");
        View.Print("- DETAILS (D)");
        View.Print("  DETAILS <keyword> or <folder> or <file>");
        View.Print("  Lists all images that have the given keyword or belong to the given folder or");
        View.Print("  given file and displays detailed information about them.");
        View.Print("- DUPLICATES (DUP, DD)");
        View.Print("  DUPLICATES <keyword> or <folder> or <file>");
        View.Print("  Finds duplicates in a set of images determined by a given parameter (comparing files byte by byte).");
        View.Print("- SCAN (S)");
        View.Print("  SCAN <keyword> or <folder> or <file>");
        View.Print("  Compares the set of images determined by the given parameter with the current state on the disk.");
    }

    private void About() {
        View.FullProgramInfo();
    }

    private void Exit() {
        if (Db.IsChanged()) {
            if (Cli == null) {
                throw new InvalidOperationException("Interpreter CLI is not initialized!");
            }

            char response = Cli.AskYesNo(View, "There are unsaved changes. Do you want to save them?", true);
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

        if (args.Length != 1) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        string path = args[0];
        switch (FileSystem.CheckPath(path)) {
            case 'F':
                AddFile(path, false);
                break;

            case 'D':
                AddDirectory(path);
                break;

            case 'E':
                StatusCode = StatusCode.PathDoesNotExist;
                View.PrintStatus(StatusCode);
                break;

            default:
                throw new InvalidOperationException("Unknown FileSystem.CheckPath() result!");
        }
    }

    private void AddFile(string filename, bool fullPath) {
        string filenameOnly = fullPath ? FileSystem.ExtractFilename(filename) : filename;
        View.Print($"Processing file '{filenameOnly}'... ", false);

        DBFile file = FileSystem.GetFileInformation(filename);

        switch (FileSystem.StatusCode) {
            case StatusCode.NoError:
                if (Db.AddFile(file) == 0) {
                    View.Print("Added.");
                } else {
                    View.Print("Updated.");
                }
                break;

            case StatusCode.FileSystemError:
                StatusCode = StatusCode.FileSystemError;
                View.Print("ERROR! (Error reading file)... Skipped.");
                break;

            case StatusCode.FileSystemNotFile:
                View.Print("WARNING! (Not a file)... Skipped.");
                break;

            case StatusCode.FileSystemNotImage:
                View.Print("WARNING! (Not an image)... Skipped.");
                break;

            default:
                throw new InvalidOperationException("Unknown FileSystem error code");
        }
    }

    private void AddDirectory(string directory) {
        View.Print($"Processing directory '{directory}'... ", false);
        List<string> listOfFiles = FileSystem.FilesInDirectory(directory);
        if (listOfFiles.Count == 0 || FileSystem.StatusCode == StatusCode.FileSystemError) {
            StatusCode = StatusCode.FileSystemError;
            View.PrintStatus(StatusCode);
            return;
        }

        View.Print($"(found {listOfFiles.Count - 1} file(s))");
        View.Print("Full path: " + listOfFiles[0]);
        for (int i = 1; i < listOfFiles.Count; i++) {
            AddFile(listOfFiles[i], true);
        }
    }

    private void AddKeyword(string[] args) {
        if (args.Length != 2) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        string keyword = args[0].ToUpper();
        string path = args[1];

        var fileIDs = Db.GetFileIDs(path, 'F');
        if (fileIDs != null) {
            View.Print($"Adding the keyword '{keyword}' to the specified file.");
        } else {
            fileIDs = Db.GetFileIDs(path, 'D');
            if (fileIDs != null) {
                View.Print($"Adding the keyword '{keyword}' to files in the specified directory.");
                View.Print($"(found {fileIDs.Count} file(s))");
            }
        }

        if (fileIDs != null) {
            foreach (int fileID in fileIDs) {
                AddKeywordToFile(keyword, fileID);
            }
        } else {
            StatusCode = StatusCode.DbFileDirDoesNotExist;
            View.PrintStatus(StatusCode);
        }
    }

    private void AddKeywordToFile(string keyword, int fileID) {
        DBFile? file = Db.GetFile(fileID);
        if (file != null) {
            View.Print($"Processing file '{file.Filename}.{file.Extension}'... ", false);
            Db.AddKeyword(keyword, fileID);
            View.Print($"Ok (fileID = {fileID}).");
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

        string path = args[0];
        var fileIDs = Db.GetFileIDs(path, 'F');
        if (fileIDs == null) {
            fileIDs = Db.GetFileIDs(path, 'D');
            if (fileIDs != null) {
                View.Print($"Processing directory '{path}'... ", false);
                View.Print($"(found {fileIDs.Count} file(s))");
            } else {
                StatusCode = StatusCode.DbFileDirDoesNotExist;
                View.PrintStatus(StatusCode);
            }
        }

        if (fileIDs != null) {
            foreach (int fileID in fileIDs.ToList()) {
                RemoveFile(fileID);
            }
        }
    }

    private void RemoveFile(int fileID) {
        DBFile? file = Db.GetFile(fileID);
        if (file != null) {
            View.Print($"Processing file '{file.Filename}.{file.Extension}'... ", false);
            Db.RemoveFile(fileID);
            View.Print("Removed.");
        }
    }

    private void RemoveKeyword(string[] args) {
        if (args.Length != 2) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        string keyword = args[0].ToUpper();
        string path = args[1];

        var fileIDs = Db.GetFileIDs(path, 'F');
        if (fileIDs != null) {
            View.Print($"Removing the keyword '{keyword}' from the specified file.");
        } else {
            fileIDs = Db.GetFileIDs(path, 'D');
            if (fileIDs != null) {
                View.Print($"Removing the keyword '{keyword}' from files in the specified directory.");
                View.Print($"(found {fileIDs.Count} file(s))");
            }
        }

        if (fileIDs != null) {
            foreach (int fileID in fileIDs) {
                RemoveKeywordFromFile(keyword, fileID);
            }
        } else {
            StatusCode = StatusCode.DbFileDirDoesNotExist;
            View.PrintStatus(StatusCode);
        }
    }

    private void RemoveKeywordFromFile(string keyword, int fileID) {
        DBFile? file = Db.GetFile(fileID);
        if (file != null) {
            View.Print($"Processing file '{file.Filename}.{file.Extension}'... ", false);
            Db.RemoveKeyword(keyword, fileID);
            View.Print($"Ok (fileID = {fileID}).");
        }
    }

    private void List(string[] args) {
        if (args.Length >= 1 && (args[0].ToUpper() == "KEYWORDS" || args[0].ToUpper() == "KEYS")) {
            ListKeywords(args[1..]);
            return;
        }

        if (args.Length >= 1 && (args[0].ToUpper() == "DIRECTORIES" || args[0].ToUpper() == "DIRS" || args[0].ToUpper() == "FOLDERS")) {
            ListDirectories(args[1..]);
            return;
        }

        List(args, false);
    }

    private void List(string[] args, bool allDetails) {
        if (args.Length == 0) {
            View.PrintDBStatistics(Db.GetDBStatistics());
            return;
        } else if (args.Length > 1) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        string path = args[0];
        char detailsLevel = ' ';
        var fileIDs = Db.GetFileIDs(path, 'F');

        if (fileIDs != null) {
            detailsLevel = allDetails ? 'A' : 'F'; // print all details or file info only
            View.Print("The specified file exists in the database.");
        } else {
            fileIDs = Db.GetFileIDs(path, 'D');
            if (fileIDs != null) {
                detailsLevel = allDetails ? 'A' : 'F'; // print all details or file info only
                View.Print("The specified directory exists in the database.");
                View.Print($"(found {fileIDs.Count} file(s))");
            } else {
                string keyword = args[0].ToUpper();
                fileIDs = Db.GetFileIDs(keyword, 'K');
                if (fileIDs != null) {
                    detailsLevel = allDetails ? 'A' : 'D'; // print all details or file info + directory name
                    View.Print("The specified keyword exists in the database.");
                    View.Print($"(found {fileIDs.Count} file(s))");
                }
            }
        }

        if (fileIDs != null) {
            foreach (int fileID in fileIDs) {
                ListFileInfo(fileID, detailsLevel);
            }
        } else {
            StatusCode = StatusCode.DbFileDirKeywordDoesNotExist;
            View.PrintStatus(StatusCode);
        }
    }

    private void ListFileInfo(int fileID, char detailsLevel) {
        DBFile file = Db.GetFile(fileID)!;
        string filenameWithExtension = $"{file.Filename}.{file.Extension}";
        string formattedTimestamp = FormatedDateTime(file.Timestamp);
        string fileSize = FileSystem.FormatFileSize(file.Size);
        string prefix = "   ";

        if (detailsLevel is 'F' or 'D') { // File info or Directory info
            if (filenameWithExtension.Length + fileSize.Length + 3 <= 60) {
                View.Print($"{filenameWithExtension,-60}   {formattedTimestamp}   {fileSize}", false);
            } else {
                View.Print($"{filenameWithExtension}   {formattedTimestamp}   {fileSize}", false);
            }

            if (file.Keywords.Contains("CHANGED")) {
                View.Print(" (CHANGED)");
            } else if (file.Keywords.Contains("DELETED")) {
                View.Print(" (DELETED)");
            } else {
                View.Print("");
            }

            if (detailsLevel == 'D') { // Directory info
                View.Print($"{prefix}in: {file.Location}");
            }

            if (file.Duplicates.Count > 0) {
                View.Print($"{prefix}Duplicates: {file.Duplicates.Count}");
            }

            if (file.PotentialDuplicates.Count > 0) {
                View.Print($"{prefix}Potential duplicates: {file.PotentialDuplicates.Count}");
            }
        } else if (detailsLevel == 'A') { // All info
            View.Print(filenameWithExtension);
            View.Print($"{prefix}in: {file.Location}");
            View.Print($"{prefix}ID: {file.ID}");
            View.Print($"{prefix}Timestamp: {formattedTimestamp}");
            View.Print($"{prefix}Size: {fileSize} ({file.Size} byte(s))");
            View.Print($"{prefix}CRC32: {file.Checksum}");

            View.Print($"{prefix}Keywords: ", false);
            foreach (string keyword in file.Keywords) {
                View.Print($"{keyword} ", false);
            }
            View.Print("");

            var duplicates = file.Duplicates;
            if (duplicates.Count > 0) {
                View.Print($"{prefix}Duplicates: {duplicates.Count}");
                foreach (int duplicateFileID in duplicates) {
                    DBFile duplicateFile = Db.GetFile(duplicateFileID)!;
                    View.Print($"{prefix}{prefix}{duplicateFile.Fullpath}");
                }
            }

            duplicates = file.PotentialDuplicates;
            if (duplicates.Count > 0) {
                View.Print($"{prefix}Potential duplicates: {duplicates.Count}");
                foreach (int duplicateFileID in duplicates) {
                    DBFile duplicateFile = Db.GetFile(duplicateFileID)!;
                    View.Print($"{prefix}{prefix}{duplicateFile.Fullpath}");
                }
            }

            View.Print($"{prefix}Metadata:");
            foreach (var metadataTag in file.Metadata) {
                View.Print($"{prefix}{prefix}{metadataTag.Directory} {metadataTag.Tag} {metadataTag.Description}");
            }
            View.Print("------------------------------------------------------");
        }
    }

    private string FormatedDateTime(string dateTime) {
        string year = dateTime[..4];
        string month = dateTime[4..6];
        string day = dateTime[6..8];
        string hour = dateTime[9..11];
        string minute = dateTime[11..13];
        string second = dateTime[13..15];
        string formattedDateTime = $"{day}.{month}.{year} {hour}:{minute}:{second}";
        return formattedDateTime;
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

    private void ListDirectories(string[] args) {
        if (args.Length > 0) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        View.Print("List of directories in the database:");
        foreach (string dir in Db.GetDirectories()) {
            View.Print($"   {dir}");
        }
    }

    private void Details(string[] args) {
        List(args, true);
    }

    private void Duplicates(string[] args) {
        if (args.Length != 1) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        string path = args[0];
        var fileIDs = Db.GetFileIDs(path, 'F');
        if (fileIDs != null) {
            View.Print("The specified file exists in the database.");
        } else {
            fileIDs = Db.GetFileIDs(path, 'D');
            if (fileIDs != null) {
                View.Print("The specified directory exists in the database.");
                View.Print($"(found {fileIDs.Count} file(s))");
            } else {
                string keyword = path.ToUpper();
                fileIDs = Db.GetFileIDs(keyword, 'K');
                if (fileIDs != null) {
                    View.Print("The specified keyword exists in the database.");
                    View.Print($"(found {fileIDs.Count} file(s))");
                }
            }
        }

        if (fileIDs != null) {
            var allDuplicatesFound = new Dictionary<int, int>();
            foreach (var fileID in fileIDs.ToList()) {
                FindDuplicates(fileID, allDuplicatesFound);
            }
            if (allDuplicatesFound.Count == 0) {
                View.Print("No duplicates found.");
            }
        } else {
            StatusCode = StatusCode.DbFileDirKeywordDoesNotExist;
            View.PrintStatus(StatusCode);
        }
    }

    private void FindDuplicates(int fileID, Dictionary<int, int> allDuplicatesFound) {
        DBFile? file = Db.GetFile(fileID);
        if (file == null) {
            return;
        }
        View.Print($"{file.Fullpath}... ", false);

        // allDuplicatesFound contains all previously found duplicates,
        // so, this allows to avoid processing already processed file IDs
        if (!allDuplicatesFound.TryGetValue(fileID, out int numOfDuplicates)) { // get number of duplicates of fileID
            // if fileID has not yet been processed
            var newDuplicatesFound = Db.ProcessDuplicates(fileID); // finds and marks duplicates of fileID
             // add found duplicates info to allDuplicatesFound
            foreach (var key_value in newDuplicatesFound) {
                allDuplicatesFound[key_value.Key] = key_value.Value;
            }
            allDuplicatesFound.TryGetValue(fileID, out numOfDuplicates);  // get number of duplicates of fileID
        }

        if (numOfDuplicates != 0) {
            View.Print($"{numOfDuplicates} duplicate(s)");
        } else {
            View.Print("no duplicates.");
        }
    }

    private void Scan(string[] args) {
        if (args.Length != 1) {
            StatusCode = StatusCode.InvalidNumberOfArguments;
            View.PrintStatus(StatusCode);
            return;
        }

        string path = args[0];
        var fileIDs = Db.GetFileIDs(path, 'F');
        if (fileIDs != null) {
            View.Print("The specified file exists in the database.");
        } else {
            fileIDs = Db.GetFileIDs(path, 'D');
            if (fileIDs != null) {
                View.Print("The specified directory exists in the database.");
                View.Print($"(found {fileIDs.Count} file(s))");
            } else {
                string keyword = args[0].ToUpper();
                fileIDs = Db.GetFileIDs(keyword, 'K');
                if (fileIDs != null) {
                    View.Print("The specified keyword exists in the database.");
                    View.Print($"(found {fileIDs.Count} file(s))");
                }
            }
        }

        if (fileIDs != null) {
            foreach (var fileID in fileIDs.ToList()) {
                ScanFile(fileID);
            }
        } else {
            StatusCode = StatusCode.DbFileDirKeywordDoesNotExist;
            View.PrintStatus(StatusCode);
        }
    }

    private void ScanFile(int fileID) {
        DBFile? oldFileInfo = Db.GetFile(fileID);
        if (oldFileInfo == null) {
            return;
        }
        View.Print($"{oldFileInfo.Fullpath}... ", false);

        var newFileInfo = FileSystem.GetFileInformation(oldFileInfo.Fullpath);

        switch (FileSystem.StatusCode) {
            case StatusCode.NoError:
                if (FileChanged(oldFileInfo, newFileInfo)) {
                    Db.AddKeyword("CHANGED", fileID);
                    Db.RemoveKeyword("DELETED", fileID);
                    View.Print("CHANGED.");
                } else {
                    Db.RemoveKeyword("CHANGED", fileID);
                    Db.RemoveKeyword("DELETED", fileID);
                    View.Print("ok.");
                }
                break;

            case StatusCode.FileSystemError:
                StatusCode = StatusCode.FileSystemError;
                View.Print("ERROR! (Error reading file)... Skipped.");
                break;

            case StatusCode.FileSystemNotFile:
                Db.AddKeyword("DELETED", fileID);
                Db.RemoveKeyword("CHANGED", fileID);
                View.Print("DELETED.");
                break;

            case StatusCode.FileSystemNotImage:
                Db.AddKeyword("CHANGED", fileID);
                Db.RemoveKeyword("DELETED", fileID);
                View.Print("CHANGED.");
                break;

            default:
                throw new InvalidOperationException("Unknown FileSystem error code");
        }
    }

    private bool FileChanged(DBFile oldFileInfo, DBFile newFileInfo) {
        var oldMetadata = oldFileInfo.Metadata;
        var newMetadata = newFileInfo.Metadata;

        if (oldFileInfo.Timestamp != newFileInfo.Timestamp ||
            oldFileInfo.Size != newFileInfo.Size ||
            oldFileInfo.Checksum != newFileInfo.Checksum ||
            oldMetadata.Count != newMetadata.Count) {
            return true;
        }

        foreach (var oldMetadataInfo in oldMetadata) {
            bool found = newMetadata.Any(newMetadataInfo =>
                oldMetadataInfo.Directory == newMetadataInfo.Directory &&
                oldMetadataInfo.Tag == newMetadataInfo.Tag &&
                oldMetadataInfo.Description == newMetadataInfo.Description);

            if (!found)
                return true;
        }

        return false;
    }
}
