using PHPIL.Tests.Engine;
using PHPIL.Tests.Engine.Execution.Functions;

namespace PHPIL.Tests;

public partial class TestUtility
{
    public static void RunAll()
    {
        var utility = new TestUtility();
        
        // ===========================
        // Tests go here
        utility.Run<CodeLexerTests>();
        utility.Run<IfElseParserTests>();
        utility.Run<ExpressionParserTests>();
        utility.Run<AnonymousFunctionParserTests>();
        utility.Run<FunctionDeclarationParserTests>();
        utility.Run<FunctionCallParserTests>();
        utility.Run<ReturnPatternTests>();
        utility.Run<ArgumentListParserTests>();
        utility.Run<ParameterListParserTests>();
        
        utility.Run<ForeachExpressionParserTests>();
        utility.Run<ForExpressionParserTests>();
        utility.Run<WhileExpressionParserTests>();
        
        utility.Run<VariableAssignmentParserTests>();
        utility.Run<LiteralPatternTests>();
        utility.Run<ArrayLiteralPatternTests>();
        utility.Run<BlockPatternTests>();
        
        // ===========================
        // =============================
        // Now for the execution tests
        // =============================
        utility.Run<FunctionCallsExecutionTests>();
        
        utility.PrintSummary();
    }
}