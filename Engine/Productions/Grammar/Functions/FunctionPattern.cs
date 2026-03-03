using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class FunctionDeclarationPattern : Pattern
{
    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        result = null;
        int start = ctx.Save();

        // Match 'function'
        if (ctx.Peek().Kind != TokenKind.Function) return false;
        ctx.Consume();
        SkipTrivia(ref ctx);

        // Match Name
        var nameToken = ctx.Peek();
        if (nameToken.Kind != TokenKind.Identifier)
        {
            ctx.Restore(start);
            return false;
        }
        ctx.Consume();
        SkipTrivia(ref ctx);

        // Match Parameter List ()
        var paramPattern = new ParameterListPattern();
        if (!paramPattern.TryMatch(ref ctx, out var pNode) || pNode is not ParameterListNode pList)
        {
            ctx.Restore(start);
            return false;
        }
        SkipTrivia(ref ctx);

        // Optional Return Type
        Token? returnType = null;
        if (ctx.Peek().Kind == TokenKind.Colon)
        {
            ctx.Consume();
            SkipTrivia(ref ctx);
            if (ctx.Peek().Kind == TokenKind.Identifier)
            {
                returnType = ctx.Consume();
            }
            SkipTrivia(ref ctx);
        }

        // Match Body Block { ... }
        // We call the pattern directly to ensure trivia is handled
        var blockPattern = new BlockPattern();
        if (!blockPattern.TryMatch(ref ctx, out var bodyNode))
        {
            ctx.Restore(start);
            return false;
        }

        result = new FunctionNode
        {
            Name = nameToken,
            Params = pList.Parameters,
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