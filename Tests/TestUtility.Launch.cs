using PHPIL.Tests.Engine;

namespace PHPIL.Tests;

public partial class TestUtility
{
    public static void RunAll()
    {
        var utility = new TestUtility();
        
        // ===========================
        // Tests go here
        utility.Run<CodeLexerTests>();
        
        // ===========================
        
        utility.PrintSummary();
    }
}