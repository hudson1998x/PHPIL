using PHPIL.Engine.Runtime;

namespace PHPIL;

public static class Program
{
    public static void Main(string[] args)
    {
        Runtime.ExecuteFile("Samples/index.php");
        GlobalRuntimeContext.Stdout.Flush();
        var output = System.Text.Encoding.UTF8.GetString(GlobalRuntimeContext.StdoutStream.ToArray());
        Console.OpenStandardOutput().Write(System.Text.Encoding.UTF8.GetBytes(output));
    }
}