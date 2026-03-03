using System.Reflection;

namespace PHPIL.Tests;

[AttributeUsage(AttributeTargets.Method)]
public class PHPILTestAttribute : Attribute { }

public partial class TestUtility
{
    private static readonly Lazy<TestUtility> _instance = new(() => new TestUtility());
    public static TestUtility Instance => _instance.Value;

    public int Passed { get; private set; }
    public int Failed { get; private set; }

    private TestUtility() { }

    public void Run<T>() where T : BaseTest, new()
    {
        var suite = new T();
        var methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                               .Where(m => m.GetCustomAttribute<PHPILTestAttribute>() != null);

        Console.WriteLine($"\n>>> Running Suite: {typeof(T).Name}");
        Console.WriteLine("--------------------------------------------------");

        foreach (var method in methods)
        {
            try
            {
                method.Invoke(suite, null);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[PASS] {method.Name}");
                Passed++;
            }
            catch (TargetInvocationException tex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FAIL] {method.Name}");
                Console.ForegroundColor = ConsoleColor.Gray;
                // tex.InnerException contains the actual assertion failure
                Console.WriteLine($"       {tex.InnerException?.Message ?? tex.Message}");
                Failed++;
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }

    public void PrintSummary()
    {
        Console.WriteLine("\n==================================================");
        Console.ForegroundColor = Failed == 0 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"TOTAL RESULTS: {Passed} Passed, {Failed} Failed");
        Console.ResetColor();
        Console.WriteLine("==================================================");
    }
}