using System.Collections.Generic;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;

namespace PHPIL.Engine.Productions.Patterns;

public class QualifiedNamePattern : Pattern
{
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

        if (ctx.Peek().Kind != TokenKind.Identifier)
        {
            if (isFullyQualified)
            {
                // Just a backslash is not a valid qualified name in most contexts
                // but let's be strict here.
                ctx.Restore(start);
                return false;
            }
            return false;
        }

        parts.Add(ctx.Consume());

        while (!ctx.IsAtEnd && ctx.Peek(0).Kind == TokenKind.NamespaceSeparator)
        {
            // Look ahead to ensure the separator is followed by an identifier
            if (ctx.Peek(1).Kind != TokenKind.Identifier) break;

            ctx.Consume(); // consume separator
            parts.Add(ctx.Consume()); // consume identifier
        }

        result = new QualifiedNameNode { Parts = parts, IsFullyQualified = isFullyQualified };
        return true;
    }
}
