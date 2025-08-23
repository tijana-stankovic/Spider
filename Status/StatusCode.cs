namespace SpiderStatus;

public enum StatusCode {
    NoError,
    UnexpectedStatus,
    UnknownCommand,
    UnknownLogCommand,
    DbFileDoesNotExist,
    DbFileDoesNotExistError,
    DbFileIncompatibleFormat,
    DbFileReadError,
    DbFileWriteError,
    InvalidNumberOfArguments,
}
