using PHPIL.Engine.Runtime;
using PHPIL.Engine.DevServer;
using PHPIL.Tests;

namespace PHPIL;

public static class Program
{
    public static void Main(string[] args)
    {
        // Run tests
        if (args.Length > 0 && args[0] == "--tests")
        {
            TestUtility.RunAll();
            return;
        }

        // Dev server mode
        if (args.Length >= 3 && args[0] == "-s")
        {
            var host = args[1];
            var entry = args[2];

            Console.WriteLine($"Starting dev server on {host}");
            Console.WriteLine($"Entry point: {entry}");

            HttpServer.Start(entry, host).GetAwaiter().GetResult();
            return;
        }

        // Execute single PHP file
        if (args.Length > 0)
        {
            Runtime.ExecuteFile(args[0]);
            Console.WriteLine(Runtime.GetExecutionResult());
        }
    }
}