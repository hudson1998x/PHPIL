using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

public class FunctionCallPattern : Pattern
{
    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        int start = ctx.Save();

        // 1. Must start with an Identifier
        if (ctx.IsAtEnd || ctx.Peek().Kind != TokenKind.Identifier) 
        { 
            result = null; 
            return false; 
        }
        var callee = new IdentifierNode { Token = ctx.Consume() };

        // 2. Must be followed by '('
        if (ctx.IsAtEnd || ctx.Peek().Kind != TokenKind.LeftParen)
        {
            ctx.Restore(start);
            result = null;
            return false;
        }

        ctx.Consume(); // consume '('

        // 3. Parse arguments
        var args = new List<ExpressionNode>();
        while (!ctx.IsAtEnd && ctx.Peek().Kind != TokenKind.RightParen)
        {
            int argStart = ctx.Save();

            if (new InnerExpressionPattern(0).TryMatch(ref ctx, out var arg))
            {
                args.Add((ExpressionNode)arg!);
            }
            else
            {
                // Climber couldn't match — this token can never become an arg.
                // Force-consume it so we don't spin, then stop collecting args.
                ctx.Consume();
                break;
            }

            // Handle comma separator
            if (!ctx.IsAtEnd && ctx.Peek().Kind == TokenKind.Comma)
                ctx.Consume();
            else
                break; // No comma and not ')' — stop, let ')' check below handle it
        }

        // Consume ')' if present; if missing, we still return a valid (partial) call node
        if (!ctx.IsAtEnd && ctx.Peek().Kind == TokenKind.RightParen)
            ctx.Consume();

        result = new FunctionCallNode
        {
            Callee = callee,
            Args = args
        };
        return true;
    }
}

namespace PHPIL.Engine.Productions
{
    public static partial class Grammar
    {
        public static FunctionCallPattern FunctionCall() => new FunctionCallPattern();
    }
}