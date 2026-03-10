using PHPIL.Engine.Runtime;

namespace PHPIL;

public static class Program
{
    public static void Main(string[] args)
    {
        Runtime.ExecuteFile("Samples/index.php");
    }
}