namespace SpiderStatus;

public enum StatusCode {
    NoError,
    UnexpectedStatus,
    UnknownCommand,
    DbFileDoesNotExist,
    DbFileIncompatibleFormat,
    DbFileReadError,
    DbFileWriteError,
    InvalidNumberOfArguments,
}
