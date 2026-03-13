using PHPIL.Engine.Runtime;
using PHPIL.Tests;

namespace PHPIL;

public static class Program
{
    public static void Main(string[] args)
    {
        // Check for --run flag
        if (args.Length > 0 && args[0] == "--run")
        {
            // Run tests
            TestUtility.RunAll();
        }
        else if (args.Length > 0)
        {
            // Run specific file
            Runtime.ExecuteFile(args[0]);
            Console.WriteLine(Runtime.GetExecutionResult());
        }
        else
        {
            // Run default tests
            TestUtility.RunAll();
        }
    }
}