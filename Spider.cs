namespace Spider;

using SpiderController;
using System;

/// <summary>
/// Represents the Spider application entry point.
/// This class provides the Main() method, which is the starting point of the application.
/// </summary>
public class Spider {

    /// <summary>
    /// The starting point of the application.
    /// This method is called when the program starts. 
    /// It initializes the controller part of the application and passes control to the controller.
    /// Only one command line parameter is supported:
    /// 'db-file-name', which is the name of the file containing the Spider database.
    /// </summary>
    /// <param name="args">The command-line arguments passed to the program.</param>
    public static void Main(string[] args) {
        if (args.Length <= 1) {
            Controller controller = new(args);
            controller.Run();
        } else {
            Console.Error.WriteLine();
            Console.Error.WriteLine("Usage: spider [db-file-name]");
        }
    }
}
