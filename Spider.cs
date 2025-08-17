namespace Spider;

using SpiderController;
using System;

public class Spider {
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
