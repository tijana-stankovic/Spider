namespace SpiderStatus;

public enum StatusCode {
    NoError,
    UnexpectedStatus,
    UnknownCommand,
    UnknownLogCommand,
    DbFileDoesNotExist,
    DbFileIncompatibleFormat,
    DbFileReadError,
    DbFileWriteError,
    InvalidNumberOfArguments,
}
