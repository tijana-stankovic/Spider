namespace SpiderStatus;

/// <summary>
/// Class for mapping status codes to their respective messages.
/// </summary>
public class StatusMessages {
    // Initializes the status messages for each status code.
    private static readonly Dictionary<StatusCode, string> statusMessages = new() {
        { StatusCode.NoError, "No error." },
        { StatusCode.UnexpectedStatus, "WARNING: Unexpected program status." },
        { StatusCode.UnknownCommand, "ERROR: Unknown command. Use Help or H for a list of available commands." },
        { StatusCode.UnknownLogCommand, "ERROR: Unknown LOG command. Use Help or H for a list of available commands." },
        { StatusCode.DbFileDoesNotExist, "WARNING: The database file does not exist. A new file has been created." },
        { StatusCode.DbFileDoesNotExistError, "ERROR: The database file does not exist. A new file will be created during the next save operation." },
        { StatusCode.DbFileIncompatibleFormat, "ERROR: The database file is in incompatible format." },
        { StatusCode.DbFileReadError, "ERROR: An error occurred while reading the database file." },
        { StatusCode.DbFileWriteError, "ERROR: An error occurred while writing to the database file." },
        { StatusCode.InvalidNumberOfArguments, "ERROR: Invalid number of arguments." },
        { StatusCode.InvalidArgument, "ERROR: Invalid argument value." },
    };

    public static string GetStatusMessage(StatusCode statusCode) {
        return statusMessages.TryGetValue(statusCode, out string? message)
            ? message
            : "WARNING: Unknown program status code.";
    }
}
