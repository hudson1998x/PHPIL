using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.OOP;

namespace PHPIL.Engine.Productions
{
    public class TraitUsePattern : Pattern
    {
        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            result = null;
            int start = ctx.Save();

            if (ctx.Peek().Kind != TokenKind.Use)
                return false;

            ctx.Consume(); // use
            Parser.SkipTrivia(ref ctx);

            var traits = new List<QualifiedNameNode>();
            while (true)
            {
                if (Grammar.QualifiedName().TryMatch(ref ctx, out var traitNode) && traitNode is QualifiedNameNode trait)
                {
                    traits.Add(trait);
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

            if (ctx.Peek().Kind == TokenKind.ExpressionTerminator)
            {
                ctx.Consume();
            }
            else if (ctx.Peek().Kind == TokenKind.LeftBrace)
            {
                ctx.Consume();
                while (!ctx.IsAtEnd && ctx.Peek().Kind != TokenKind.RightBrace)
                {
                    ctx.Consume();
                    Parser.SkipTrivia(ref ctx);
                }
                if (ctx.Peek().Kind == TokenKind.RightBrace) ctx.Consume();
            }
            else
            {
                ctx.Restore(start);
                return false;
            }

            result = new TraitUseNode { Traits = new List<SyntaxNode>(traits) };
            return true;
        }
    }
}
