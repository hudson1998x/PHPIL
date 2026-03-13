using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class TokenPattern : Pattern
{
    private readonly TokenKind _expectedKind;
    private readonly Func<Token, SyntaxNode> _executor;

    public TokenPattern(TokenKind kind, Func<Token, SyntaxNode> executor)
    {
        _expectedKind = kind;  
        _executor = executor;
    } 

    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        result = null;
        if (!ctx.IsAtEnd && ctx.Peek().Kind == _expectedKind)
        {
            var token = ctx.Consume();
            // Optionally wrap the token in a simple TerminalNode
            result = _executor(token);
            return true;
        }
        
        // Record failure if token doesn't match
        var current = ctx.Peek();
        ctx.RecordFailure(ctx.Position, $"TokenPattern({_expectedKind})", _expectedKind.ToString());
        return false;
    }
}