using System;
using System.Collections.Generic;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.OOP;
using PHPIL.Engine.Productions.Patterns;

namespace PHPIL.Engine.Productions
{
    public class InterfacePattern : Pattern
    {
        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            result = null;
            int start = ctx.Save();

            if (ctx.Peek().Kind != TokenKind.Interface)
            {
                return false;
            }

            ctx.Consume(); // interface
            Parser.SkipTrivia(ref ctx);

            if (!Grammar.QualifiedName().TryMatch(ref ctx, out var nameNode) || nameNode is not QualifiedNameNode name)
            {
                ctx.Restore(start);
                return false;
            }

            Parser.SkipTrivia(ref ctx);

            List<QualifiedNameNode> extends = new();
            if (ctx.Peek().Kind == TokenKind.Extends)
            {
                ctx.Consume();
                Parser.SkipTrivia(ref ctx);

                while (true)
                {
                    if (Grammar.QualifiedName().TryMatch(ref ctx, out var extNode) && extNode is QualifiedNameNode ext)
                    {
                        extends.Add(ext);
                    }
                    else
                    {
                        ctx.Restore(start);
                        return false;
                    }
                    Parser.SkipTrivia(ref ctx);
                    if (ctx.Peek().Kind == TokenKind.Comma)
                    {
                        ctx.Consume();
                        Parser.SkipTrivia(ref ctx);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (ctx.Peek().Kind != TokenKind.LeftBrace)
            {
                ctx.Restore(start);
                return false;
            }

            ctx.Consume(); // {
            Parser.SkipTrivia(ref ctx);

            var members = new List<SyntaxNode>();
            while (!ctx.IsAtEnd && ctx.Peek().Kind != TokenKind.RightBrace)
            {
                Parser.SkipTrivia(ref ctx);
                if (ctx.Peek().Kind == TokenKind.RightBrace) break;

                if (new ConstantPattern().TryMatch(ref ctx, out var constant))
                {
                    members.Add(constant);
                }
                else if (new MethodPattern().TryMatch(ref ctx, out var method))
                {
                    members.Add(method);
                }
                else
                {
                    var t = ctx.Peek();
                    throw new Exception($"Unexpected token in interface body: {t.Kind} at {t.Start}");
                }
                Parser.SkipTrivia(ref ctx);
            }

            if (ctx.Peek().Kind != TokenKind.RightBrace)
            {
                ctx.Restore(start);
                return false;
            }
            ctx.Consume(); // }

            result = new InterfaceNode { Name = name, Extends = extends, Members = members };
            return true;
        }
    }
}
