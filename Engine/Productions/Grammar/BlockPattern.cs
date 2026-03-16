using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.Runtime;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns
{
    public class BlockPattern : Pattern
    {
        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            result = null;
            int start = ctx.Save();

            // 1. Skip leading newlines/spaces before the '{'
            SkipTrivia(ref ctx);

            if (ctx.Peek().Kind != TokenKind.LeftBrace) 
            {
                ctx.Restore(start);
                return false;
            }
            ctx.Consume(); // Consumes '{'

            var block = new BlockNode();

            // Skip any trivia after opening brace
            SkipTrivia(ref ctx);

            // 2. Parse statements until '}'
            while (!ctx.IsAtEnd && ctx.Peek().Kind != TokenKind.RightBrace)
            {
                // Use your Parser's logic to get a single statement
                var node = Parser.ParseSingle(ref ctx);
                if (node != null)
                {
                    block.Statements.Add(node);
                }
                else
                {
                    // If we can't parse a statement, we must move forward to avoid infinite loops
                    ctx.Consume();
                }
            
                SkipTrivia(ref ctx);
            }

            // 3. Match the closing '}'
            if (ctx.Peek().Kind != TokenKind.RightBrace)
            {
                ctx.Restore(start);
                return false;
            }
            ctx.Consume();

            result = block;
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
}

namespace PHPIL.Engine.Productions
{
    public static partial class Grammar
    {
        public static BlockPattern Block()
        {
            return new BlockPattern();
        }
    }
}