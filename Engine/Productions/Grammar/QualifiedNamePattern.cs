using System.Collections.Generic;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;

namespace PHPIL.Engine.Productions.Patterns;

public class QualifiedNamePattern : Pattern
{
    private static readonly HashSet<TokenKind> FunctionKeywords =
    [
        TokenKind.Die,
        TokenKind.Print,
        TokenKind.Echo,
        TokenKind.Include,
        TokenKind.IncludeOnce,
        TokenKind.Require,
        TokenKind.RequireOnce,
        TokenKind.Unset,
        TokenKind.List,
        TokenKind.Array,
    ];

    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        int start = ctx.Save();
        result = null;
        var parts = new List<Token>();
        bool isFullyQualified = false;

        if (ctx.Peek().Kind == TokenKind.NamespaceSeparator)
        {
            isFullyQualified = true;
            ctx.Consume();
        }

        var currentKind = ctx.Peek().Kind;
        if (currentKind != TokenKind.Identifier && !FunctionKeywords.Contains(currentKind))
        {
            if (isFullyQualified)
            {
                ctx.Restore(start);
                return false;
            }
            return false;
        }

        parts.Add(ctx.Consume());

        while (!ctx.IsAtEnd && ctx.Peek(0).Kind == TokenKind.NamespaceSeparator)
        {
            // Look ahead to ensure the separator is followed by an identifier or function keyword
            var nextKind = ctx.Peek(1).Kind;
            if (nextKind != TokenKind.Identifier && !FunctionKeywords.Contains(nextKind)) break;

            ctx.Consume(); // consume separator
            parts.Add(ctx.Consume()); // consume identifier
        }

        result = new QualifiedNameNode { Parts = parts, IsFullyQualified = isFullyQualified };
        return true;
    }
}
