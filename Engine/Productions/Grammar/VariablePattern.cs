using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class VariablePattern : Pattern
{
    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        result = null;
        
        if (ctx.Peek().Kind != TokenKind.Variable)
        {
            return false;
        }

        var varToken = ctx.Consume();
        result = new VariableNode { Token = varToken };
        return true;
    }
}
