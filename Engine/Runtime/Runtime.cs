using System.Text;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.SemanticAnalysis;

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
        var tokens = Lexer.ParseSpan(fileContent);
        
        var builder = new StringBuilder();

        // builder.AppendLine("Tokens: [");
        // foreach (var token in tokens)
        // {
        //     token.ToJson(in fileContent, builder);
        // }
        // builder.AppendLine("]");
        //
        // Console.WriteLine(builder.ToString());
        
        var span = (ReadOnlySpan<Token>) tokens.AsSpan();
        var ast = Parser.Parse(in tokens, in fileContent);
        
        var visitors = new Visitor(
            new SemanticVisitor(),
            new Compiler()
        );

        
        
        
        ast?.Accept(visitors, in fileContent);
        
        ast.ToJson(in fileContent, in span, builder);
        Console.WriteLine(builder.ToString());
    }
}