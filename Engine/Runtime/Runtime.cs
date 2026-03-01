using System.Text;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;

namespace PHPIL.Engine.Runtime;

public static class Runtime
{
    public static void ExecuteFile(string filePath)
    {
        var path   = Path.GetFullPath(filePath);
        var source = File.ReadAllText(path).AsSpan();
        Execute(in source, path);
    }

    public static void Execute(in ReadOnlySpan<char> fileContent, string fileName = "vm:0")
    {
        var tokens = (ReadOnlySpan<Token>)Lexer.ParseSpan(fileContent).AsSpan();
        var ast = Parser.Parse(in tokens, in fileContent, 0);

        // var builder = new StringBuilder();
        // ast.ToJson(in fileContent, in tokens, builder);
        // Console.WriteLine(builder.ToString());

        var executor = new IlProducer();
        
        try
        {
            executor.Visit(ast, in fileContent);
            executor.Execute();
        }
        catch (Exception e)
        {
            GlobalRuntimeContext.Stdout.Write("Exception during execution: " + e);
            GlobalRuntimeContext.Stdout.Write("IL log:");
            GlobalRuntimeContext.Stdout.Write(executor.GetILGenerator().GetLog());
        }
    }
}