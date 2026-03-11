using System.Collections.Generic;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;

namespace PHPIL.Engine.Productions.Patterns;

public class UsePattern : Pattern
{
    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        int start = ctx.Save();
        result = null;

        if (ctx.Peek().Kind != TokenKind.Use) return false;
        ctx.Consume();
        Parser.SkipTrivia(ref ctx);

        var imports = new List<UseImport>();

        while (!ctx.IsAtEnd)
        {
            if (!new QualifiedNamePattern().TryMatch(ref ctx, out var qnameNode)) break;
            var qname = qnameNode as QualifiedNameNode;

            Parser.SkipTrivia(ref ctx);
            Token? alias = null;

            if (ctx.Peek().Kind == TokenKind.As)
            {
                ctx.Consume();
                Parser.SkipTrivia(ref ctx);
                if (ctx.Peek().Kind == TokenKind.Identifier)
                {
                    alias = ctx.Consume();
                }
            }

            imports.Add(new UseImport { Name = qname!, Alias = alias });
            Parser.SkipTrivia(ref ctx);

            if (ctx.Peek().Kind == TokenKind.Comma)
            {
                ctx.Consume();
                Parser.SkipTrivia(ref ctx);
            }
            else break;
        }

        if (ctx.Peek().Kind == TokenKind.ExpressionTerminator)
        {
            ctx.Consume();
            result = new UseNode { Imports = imports };
            return true;
        }

        ctx.Restore(start);
        return false;
    }
}
