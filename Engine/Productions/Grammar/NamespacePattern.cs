using System.Collections.Generic;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;

namespace PHPIL.Engine.Productions.Patterns;

public class NamespacePattern : Pattern
{
    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        int start = ctx.Save();
        result = null;

        if (ctx.Peek().Kind != TokenKind.Namespace) return false;
        ctx.Consume();
        Parser.SkipTrivia(ref ctx);

        QualifiedNameNode? name = null;
        if (ctx.Peek().Kind == TokenKind.Identifier || ctx.Peek().Kind == TokenKind.NamespaceSeparator)
        {
            if (new QualifiedNamePattern().TryMatch(ref ctx, out var qname))
            {
                name = qname as QualifiedNameNode;
            }
        }

        Parser.SkipTrivia(ref ctx);

        if (ctx.Peek().Kind == TokenKind.ExpressionTerminator)
        {
            ctx.Consume();
            result = new NamespaceNode { Name = name };
            return true;
        }

        if (ctx.Peek().Kind == TokenKind.LeftBrace)
        {
            ctx.Consume();
            var statements = new List<SyntaxNode>();
            while (!ctx.IsAtEnd && ctx.Peek().Kind != TokenKind.RightBrace)
            {
                var stmt = Parser.ParseSingle(ref ctx);
                if (stmt != null) statements.Add(stmt);
                Parser.SkipTrivia(ref ctx);
                if (!ctx.IsAtEnd && ctx.Peek().Kind == TokenKind.ExpressionTerminator) ctx.Consume();
            }

            if (!ctx.IsAtEnd && ctx.Peek().Kind == TokenKind.RightBrace)
            {
                ctx.Consume();
                result = new NamespaceNode { Name = name, Statements = statements };
                return true;
            }
        }

        ctx.Restore(start);
        return false;
    }
}
