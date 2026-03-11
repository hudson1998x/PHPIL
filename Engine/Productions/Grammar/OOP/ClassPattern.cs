using System;
using System.Collections.Generic;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.OOP;
using PHPIL.Engine.Productions.Patterns;

namespace PHPIL.Engine.Productions
{
    public class ClassPattern : Pattern
    {
        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            SyntaxNode? node = null; result = null;
            int start = ctx.Save();

            bool isAbstract = false;
            bool isFinal = false;

            // Optional modifiers
            if (ctx.Peek().Kind == TokenKind.Abstract)
            {
                isAbstract = true;
                ctx.Consume();
                Parser.SkipTrivia(ref ctx);
            }
            else if (ctx.Peek().Kind == TokenKind.Final)
            {
                isFinal = true;
                ctx.Consume();
                Parser.SkipTrivia(ref ctx);
            }

            if (ctx.Peek().Kind != TokenKind.Class)
            {
                ctx.Restore(start);
                return false;
            }

            ctx.Consume(); // class
            Parser.SkipTrivia(ref ctx);

            if (!Grammar.QualifiedName().TryMatch(ref ctx, out var nameNode) || nameNode is not QualifiedNameNode name)
            {
                ctx.Restore(start);
                return false;
            }

            Parser.SkipTrivia(ref ctx);

            QualifiedNameNode? extends = null;
            if (ctx.Peek().Kind == TokenKind.Extends)
            {
                ctx.Consume();
                Parser.SkipTrivia(ref ctx);
                if (Grammar.QualifiedName().TryMatch(ref ctx, out var extNode) && extNode is QualifiedNameNode ext)
                {
                    extends = ext;
                }
                else
                {
                    ctx.Restore(start);
                    return false;
                }
                Parser.SkipTrivia(ref ctx);
            }

            List<QualifiedNameNode> implements = new();
            if (ctx.Peek().Kind == TokenKind.Implements)
            {
                ctx.Consume();
                Parser.SkipTrivia(ref ctx);

                while (true)
                {
                    if (Grammar.QualifiedName().TryMatch(ref ctx, out var impNode) && impNode is QualifiedNameNode imp)
                    {
                        implements.Add(imp);
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

                if (new TraitUsePattern().TryMatch(ref ctx, out var traitUse))
                {
                    members.Add(traitUse);
                }
                else if (new ConstantPattern().TryMatch(ref ctx, out var constant))
                {
                    members.Add(constant);
                }
                else if (new MethodPattern().TryMatch(ref ctx, out var method))
                {
                    members.Add(method);
                }
                else if (new PropertyPattern().TryMatch(ref ctx, out var property))
                {
                    members.Add(property);
                }
                else
                {
                    // Fallback or error? For now, if we can't match anything, it's a parse error or unknown token
                    var t = ctx.Peek();
                    throw new Exception($"Unexpected token in class body: {t.Kind} at {t.Start}");
                }
                Parser.SkipTrivia(ref ctx);
            }

            if (ctx.Peek().Kind != TokenKind.RightBrace)
            {
                ctx.Restore(start);
                return false;
            }
            ctx.Consume(); // }

            result = node;
            return true;
        }
    }
}
