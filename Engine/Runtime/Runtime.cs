using System.Text;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;

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
        var span = (ReadOnlySpan<Token>) tokens.AsSpan();
        var ast = Parser.Parse(in tokens, in fileContent);
        
        
    }
}