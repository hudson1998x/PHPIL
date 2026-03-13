using PHPIL.Engine.Runtime;
using PHPIL.Tests;

namespace PHPIL;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            Runtime.ExecuteFile("Samples/index.php");
            Console.WriteLine(Runtime.GetExecutionResult());
        }
        else
        {
            TestUtility.RunAll();
        }
    }
}