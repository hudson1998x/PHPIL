using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;


public class FunctionCallPattern : Pattern
{
    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        int start = ctx.Save();
        ExpressionNode? callee = null;

        // 1. Try to match a QualifiedName (for named function calls)
        if (Grammar.QualifiedName().TryMatch(ref ctx, out var qnameNode))
        {
            callee = qnameNode as ExpressionNode;
        }
        // 2. Try to match a Variable (for closure calls like $handle(...))
        else if (ctx.Peek().Kind == TokenKind.Variable)
        {
            // Parse the variable
            if (Grammar.Variable().TryMatch(ref ctx, out var varNode))
            {
                callee = varNode as ExpressionNode;
            }
        }

        // If neither matched, this is not a function call
        if (callee == null)
        {
            ctx.Restore(start);
            result = null;
            return false;
        }

        Parser.SkipTrivia(ref ctx);

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
            Parser.SkipTrivia(ref ctx);

            if (new InnerExpressionPattern(0).TryMatch(ref ctx, out var arg))
            {
                args.Add((ExpressionNode)arg!);
            }
            else
            {
                break;
            }

            Parser.SkipTrivia(ref ctx);

            // Handle comma separator
            if (!ctx.IsAtEnd && ctx.Peek().Kind == TokenKind.Comma)
            {
                ctx.Consume();
            }
            else
            {
                break;
            }
        }

        Parser.SkipTrivia(ref ctx);

        // Consume ')'
        if (!ctx.IsAtEnd && ctx.Peek().Kind == TokenKind.RightParen)
            ctx.Consume();
        else
        {
            // If we're missing the closing paren, it's usually a failure in strict matching
            ctx.Restore(start);
            result = null;
            return false;
        }

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