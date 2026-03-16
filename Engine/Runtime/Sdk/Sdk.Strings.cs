using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.Runtime.Sdk;

public static partial class SdkInitializer
{
    static void InitStrings()
    {
        Sdk.Function("die")
            .Takes<object?>()
            .Calls(Strings.Die);
            
        Sdk.Function("str_replace")
            .Takes<string, string, string>()
            .Returns<string>()
            .Calls(Strings.StrReplace);
    }
}

public static class Strings
{
    public static void Die(object? value)
    {
        if (value != null)
            Streams.Print(value);
        
        // Flush the appropriate stream based on execution context
        var context = Runtime.CurrentContext;
        if (context != null)
            context.OutputStream.Flush();
        else
            SdkInitializer.StdoutStream.Flush();
        
        throw new DieException();
    }
    
    public static string StrReplace(string search, string replace, string subject)
    {
        return subject.Replace(search, replace);
    }
}

public class DieException : Exception
{
    public DieException() : base("die() called") { }
}
