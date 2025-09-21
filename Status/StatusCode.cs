namespace SpiderStatus;

/// <summary>
/// Enum representing various status codes for the Spider application.
/// </summary>
public enum StatusCode {
    /// <summary>
    /// No error.
    /// </summary>
    NoError,

    /// <summary>
    /// Unexpected program status.
    /// </summary>
    UnexpectedStatus,

    /// <summary>
    /// Unknown command.
    /// </summary>
    UnknownCommand,

    /// <summary>
    /// Unknown LOG command.
    /// </summary>
    UnknownLogCommand,

    /// <summary>
    /// Warning: The database file does not exist.
    /// A new file has been created.
    /// </summary>
    DbFileDoesNotExist,

    /// <summary>
    /// Error: The database file does not exist.
    /// A new file will be created during the next save operation.
    /// </summary>
    DbFileDoesNotExistError,

    /// <summary>
    /// The database file is in incompatible format.
    /// </summary>
    DbFileIncompatibleFormat,

    /// <summary>
    /// An error occurred while reading the database file.
    /// </summary>
    DbFileReadError,

    /// <summary>
    /// An error occurred while writing to the database file.
    /// </summary>
    DbFileWriteError,


    /// <summary>
    /// The Spider command has an invalid number of arguments.
    /// </summary>
    InvalidNumberOfArguments,

    /// <summary>
    /// The Spider command has an invalid argument value.
    /// </summary>
    InvalidArgument,
}
