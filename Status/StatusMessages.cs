namespace PhotoStatus;

public class StatusMessages {
    private static readonly Dictionary<StatusCode, string> statusMessages = new() {
        { StatusCode.NoError, "No error." },
        { StatusCode.UnexpectedStatus, "WARNING: Unexpected program status." },
        { StatusCode.UnknownCommand, "ERROR: Unknown command. Use Help or H for a list of available commands." },
        { StatusCode.DbFileDoesNotExist, "WARNING: The database file does not exist. A new file will be created." },
        { StatusCode.DbPathDoesNotExist, "ERROR: The path does not exists in the database." },
        { StatusCode.DbFileDirDoesNotExist, "ERROR: The file or directory does not exist in the database." },
        { StatusCode.DbFileDirKeywordDoesNotExist, "ERROR: The file, directory, or keyword does not exist in the database." },
        { StatusCode.DbFileIncompatibleFormat, "ERROR: The database file is in incompatible format." },
        { StatusCode.DbFileReadError, "ERROR: An error occurred while reading the database file." },
        { StatusCode.DbFileWriteError, "ERROR: An error occurred while writing to the database file." },
        { StatusCode.InvalidNumberOfArguments, "ERROR: Invalid number of arguments." },
        { StatusCode.PathDoesNotExist, "ERROR: Path does not exists." },
        { StatusCode.FileSystemError, "ERROR: Error reading file system." },
        { StatusCode.FileSystemNotFile, "WARNING: Not a file." },
        { StatusCode.FileSystemNotImage, "WARNING: File is not an image." }
    };

    public static string GetStatusMessage(StatusCode statusCode) {
        return statusMessages.TryGetValue(statusCode, out string? message)
            ? message
            : "WARNING: Unknown program status code.";
    }
}
