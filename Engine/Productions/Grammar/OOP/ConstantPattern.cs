using System;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure.OOP;
using PHPIL.Engine.Productions.Patterns;

namespace PHPIL.Engine.Productions
{
    public class ConstantPattern : Pattern
    {
        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            SyntaxNode? node = null; result = null;
            int start = ctx.Save();

            PhpModifiers modifiers = PhpModifiers.None;
            while (true)
            {
                var kind = ctx.Peek().Kind;
                if (kind == TokenKind.Public) modifiers |= PhpModifiers.Public;
                else if (kind == TokenKind.Protected) modifiers |= PhpModifiers.Protected;
                else if (kind == TokenKind.Private) modifiers |= PhpModifiers.Private;
                else break;

                ctx.Consume();
                Parser.SkipTrivia(ref ctx);
            }

            if (ctx.Peek().Kind != TokenKind.Const)
            {
                ctx.Restore(start);
                return false;
            }

            ctx.Consume(); // const
            Parser.SkipTrivia(ref ctx);

            if (ctx.Peek().Kind != TokenKind.Identifier)
            {
                ctx.Restore(start);
                return false;
            }

            var name = new IdentifierNode { Token = ctx.Consume() };
            Parser.SkipTrivia(ref ctx);

            if (ctx.Peek().Kind != TokenKind.AssignEquals)
            {
                ctx.Restore(start);
                return false;
            }

            ctx.Consume(); // =
            Parser.SkipTrivia(ref ctx);

            if (!Grammar.Expressions.Inner().TryMatch(ref ctx, out var value))
            {
                ctx.Restore(start);
                return false;
            }

            Parser.SkipTrivia(ref ctx);
            if (ctx.Peek().Kind != TokenKind.ExpressionTerminator)
            {
                ctx.Restore(start);
                return false;
            }
            ctx.Consume(); // ;

            result = new ConstantNode { Modifiers = modifiers, Name = name, Value = value };
            return true;
        }
    }
}
