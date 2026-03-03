using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class UseListPattern : Pattern
{
    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        var captures = new List<UseCapture>();
        int start = ctx.Save();

        if (ctx.Peek().Kind != TokenKind.LeftParen)
        {
            result = null;
            return false;
        }
        ctx.Consume();

        while (!ctx.IsAtEnd && ctx.Peek().Kind != TokenKind.RightParen)
        {
            SkipTrivia(ref ctx);
            bool isByRef = false;
            if (ctx.Peek().Kind == TokenKind.Ampersand)
            {
                isByRef = true;
                ctx.Consume();
                SkipTrivia(ref ctx);
            }

            if (ctx.Peek().Kind != TokenKind.Variable)
            {
                ctx.Restore(start);
                result = null;
                return false;
            }

            captures.Add(new UseCapture { Name = ctx.Consume(), ByRef = isByRef });
            SkipTrivia(ref ctx);

            if (ctx.Peek().Kind == TokenKind.Comma)
            {
                ctx.Consume();
            }
        }

        if (ctx.Peek().Kind != TokenKind.RightParen) { ctx.Restore(start);
            result = null; return false; }
        ctx.Consume();

        result = new UseCaptureListNode { Captures = captures };
        return true;
    }

    private void SkipTrivia(ref ParserContext ctx)
    {
        while (!ctx.IsAtEnd && (ctx.Peek().Kind == TokenKind.Whitespace || ctx.Peek().Kind == TokenKind.NewLine))
        {
            ctx.Consume();
        }
    }
}

public class UseCaptureListNode : SyntaxNode 
{ 
    public List<UseCapture> Captures { get; init; } = [];
}