using PHPIL.Engine.Runtime;
using PHPIL.Tests;

namespace PHPIL;

public static class Program
{
    public static void Main(string[] args)
    {
        // Runtime.ExecuteFile("Samples/index.php");
        // Console.WriteLine(Runtime.GetExecutionResult());
        
        TestUtility.RunAll();
    }
}