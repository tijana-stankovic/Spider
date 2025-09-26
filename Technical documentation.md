# Project Spider - Technical documentation 

## List of source files  
- **Spider.cs**  
  It contains the Spider class with the Main() method.  
  This is the Spider application entry point.  

- **Controller.cs**  
  It contains the top-level class of the Controller part of the Spider application.  
  Here, we can find the `Run()` method, which is the main application loop.  

- **CmdInterpreter.cs**  
  The application's core module includes methods responsible for interpreting all user commands.

- **CLI.cs**  
  It contains Command Line Interface (CLI) class with methods for handling user input.

- **Command.cs**  
  Internal supporting structure for a user command.

- **DB.cs**  
  It contains the top-level class of the Spider database implementation.  
  It provides methods for accessing the Spider database as a whole.

- **DBData.cs**  
  It contains class that encapsulates the internal representation of the Spider database.
  It maintains internal structures and provides methods for manipulating data (pages, starting point names, keywords, etc.) in the database.

- **DBPage.cs**  
  Internal supporting structure for a web page (URL) - keywords connection.

- **DBStartingPoint.cs**  
  Internal supporting structure for a Starting Point.

- **View.cs**  
  It contains a class with methods for displaying various information to the user and performing log data operations.

- **Http.cs**  
  Contains all classes related to web crawling processing.

- **StatusCode.cs**  
  Defines all application status codes.

- **StatusMessages.cs**  
  Provides various messages corresponding to application status codes.



## Application structure  
The Spider application was developed using the Model-View-Controller (MVC) design pattern.

- Starting poinf of the application  
    - Spider.cs  
- Controller part 
    - Controller.cs  
    - CmdInterpreter.cs  
    - CLI.cs  
    - Command.cs  
- Model part  
    - DB.cs
    - DBData.cs
    - DBPage.cs
    - DBStartingPoint.cs
- View part  
    - View.cs  
- Other (supporting) parts  
    - Http.cs  
    - StatusCode.cs  
    - StatusMessages.cs  



## List of classes  

- **Spider class**  
    File: Spider.cs.  
    Represents the Spider application entry point.  
    This class provides the `Main()` method, which is the starting point of the application.  

- **Controller class**  
    File: Controller.cs  
    This is the top-level class of the Controller.  
    It initializes the other application and Controller parts and executes the main application loop.  
    The main application loop is the method `Run()`.  
    It processes user commands until the "quit signal" (END command) is received.  

- **CmdInterpreter class**  
    File: CmdInterpreter.cs  
    This is the Command Interpreter - the main processing class of the Controller.  
    It executes all commands of the Spider application and is responsible for communication with other parts.  
    Tha main method of this class is the `ExecuteCommand()` method.  
    This is the entry point for command processing and executes the specified command.  
    Every user command has its corresponding method here:  

    **Command -> Method name**:
    ```
    - HELP -> Help()
    - ABOUT -> About()
    - EXIT -> Exit()
    - SAVE -> Save(<args>)
    - ADD -> Add(<args>)
    - AK -> AddKeyword(<args>)
    - REMOVE -> Remove(<args>)
    - RK -> RemoveKeyword(<args>)
    - LIST -> List(<args>)
    - LK -> ListKeywords(<args>)
    - LN -> ListStartingPoints(<args>)
    - PSCAN -> PScan(<args>)
    - SCAN -> Scan(<args>)
    - SCANK -> ScanKeywords(<args>)
    - FIND -> Find(<args>)
    - LOG -> Log(<args>)
    ```                

- **CLI class**  
    File: CLI.cs  
    Command Line Interface (CLI) class with methods for handling user input.  
    Key methods: `ReadCommand()`, `AskYesNo()`  

- **Command class**  
    File: Command.cs  
    A class providing internal supporting structure for a user command.  

- **DB class**  
    File: DB.cs  
    The top-level class of the Spider database implementation.  
    It provides methods for accessing the Spider database as a whole.  
    Key methods: `ReadDB()`, `WriteDB()`, `AddPage()`, `RemovePage()`, `AddStartingPoint()`, `RemoveStartingPoint()`, `AddKeyword()`, `RemoveKeyword()`  

- **DBData class**  
    File: DBData.cs  
    This class contains the internal representation of the Spider database.  
    It contains the internal structures and provides methods for manipulating data (pages, starting point names, keywords, etc.) in the database.  
    Key structures (type: Dictionary): `Pages`, `URLs`, `NameToPages`, `KeywordToPages`, `Keywords`, `SPNames`

- **DBPage class**  
    File: DBPage.cs  
    This class contains the internal representation of the DBPage object.  
    It contains the internal structures and provides methods for manipulating DBPage data.   

- **StartingPoint class**  
    File: DBStartingPoint.cs  
    This class contains the internal representation of the starting point object.  
    It contains the internal structures and provides methods for manipulating starting point data.  

- **View class**  
    File: View.cs
    A class with methods for displaying various information to the user and performing log data operations.
    Key methods: `Print()`, `LogOpen()`, `LogClose()`, `LogPrint()`

- **HttpWebCrawler class**  
    File: Http.cs  
    A class with methods for performing sequential web crawling.  
    Key methods: `Crawl()`, `FetchPage()`  

- **PWebCrawler class**  
    File: Http.cs  
    A class with methods for performing parallel web crawling.  
    Key methods: `Crawl()`, `ProcessURL()`, `FetchPage()`  

- **CrawlResult class**  
    File: Http.cs  
    Represents the result of a web crawling operation.  

- **StatusMessages class**  
    File: StatusMessages.cs  
    Class for mapping status codes to their respective messages.  



## List of key methods  
- `Main()` in the class Spider  
    The starting point of the application.  
    This method is called when the program starts.   
    It initializes the controller part of the application and passes control to the controller.  

- `Run()` in the class Controller  
    This is the main application loop.  
    Processes user commands until the "quit signal" (END command) is received.  

- `ExecuteCommand()` in the class CmdInterpreter  
    Entry point for command processing. Executes the specified command.  

- `ReadCommand()` in the class CLI  
    Reads a command from the user input.  
    Parses the input line into a command and its arguments.  

- `AskYesNo()` in the class CLI  
    Asks the user a yes/no question and returns the response.  

- `ReadDB()` in the class DB  
    Reads Spider database from the file.  

- `WriteDB()` in the class DB  
    Writes internal Spider database to the the file.    

- `Crawl()` in the class WebCrawler  
    Main crawling function for sequential web crawling.  

- `Crawl()` + `ProcessURL()` in the class PWebCrawler  
    Main crawling function for parallel web crawling.  
    This function run parallel threads to process URLs from the queue.  



## List of key structures of the spider internal database

**All in the DBData class.**  

```
    // index: page ID -> page (this is the place all pages are stored) (1:1)
    Dictionary<int, DBPage> Pages 

    // index: full, unique page URL -> page ID (1:1)
    Dictionary<string, int> URLs

    // index: starting point name -> page ID (1:N)
    Dictionary<string, HashSet<int>> NameToPages

    // index: keyword -> page ID (1:N)
    Dictionary<string, HashSet<int>> KeywordToPages

    // all the keywords the user is searching for (the user is interested in)
    // keyword.upper() -> the original keyword entered by the user
    Dictionary<string, string> Keywords

    // starting point names
    Dictionary<string, StartingPoint> SPNames 
```    