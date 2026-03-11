using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.OOP;

namespace PHPIL.Engine.Productions
{
    public class TraitPattern : Pattern
    {
        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            result = null;
            int start = ctx.Save();

            if (ctx.Peek().Kind != TokenKind.Trait)
            {
                return false;
            }

            ctx.Consume(); // trait
            Parser.SkipTrivia(ref ctx);

            if (!Grammar.QualifiedName().TryMatch(ref ctx, out var nameNode) || nameNode is not QualifiedNameNode name)
            {
                ctx.Restore(start);
                return false;
            }

            Parser.SkipTrivia(ref ctx);

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

                if (new MethodPattern().TryMatch(ref ctx, out var method))
                {
                    members.Add(method);
                }
                else if (new PropertyPattern().TryMatch(ref ctx, out var property))
                {
                    members.Add(property);
                }
                else
                {
                    var t = ctx.Peek();
                    throw new Exception($"Unexpected token in trait body: {t.Kind} at {t.Start}");
                }
                Parser.SkipTrivia(ref ctx);
            }

            if (ctx.Peek().Kind != TokenKind.RightBrace)
            {
                ctx.Restore(start);
                return false;
            }
            ctx.Consume(); // }

            result = new TraitNode { Name = name, Members = members };
            return true;
        }
    }
}
