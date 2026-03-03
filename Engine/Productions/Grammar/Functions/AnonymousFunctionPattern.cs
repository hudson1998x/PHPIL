using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class AnonymousFunctionPattern : Pattern
{
    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        result = null;
        int start = ctx.Save();

        if (ctx.Peek().Kind != TokenKind.Function) return false;
        ctx.Consume();
        SkipTrivia(ref ctx);

        if (!new ParameterListPattern().TryMatch(ref ctx, out var pNode) || pNode is not ParameterListNode pList)
        {
            ctx.Restore(start);
            return false;
        }
        SkipTrivia(ref ctx);

        List<UseCapture> captures = new();
        if (ctx.Peek().Kind == TokenKind.Use)
        {
            ctx.Consume();
            SkipTrivia(ref ctx);
            // Assuming UseListPattern is implemented similarly
            if (Productions.Grammar.UseList().TryMatch(ref ctx, out var uNode) && uNode is UseCaptureListNode uList)
            {
                captures = uList.Captures;
            }
            SkipTrivia(ref ctx);
        }

        Token? returnType = null;
        if (ctx.Peek().Kind == TokenKind.Colon)
        {
            ctx.Consume();
            SkipTrivia(ref ctx);
            returnType = ctx.Consume();
            SkipTrivia(ref ctx);
        }

        if (!Productions.Grammar.Block().TryMatch(ref ctx, out var bodyNode))
        {
            ctx.Restore(start);
            return false;
        }

        result = new AnonymousFunctionNode
        {
            Params = pList.Parameters,
            UseCaptures = captures,
            ReturnType = returnType,
            Body = (BlockNode?)bodyNode
        };

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