
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions
{
    public static class Parser
    {
        public static SyntaxNode? Parse(in Token[] tokens, in ReadOnlySpan<char> source)
        {
            var ctx = new ParserContext(tokens.AsSpan(), source);
            var root = new BlockNode();

            while (!ctx.IsAtEnd)
            {
                SkipTrivia(ref ctx);
                if (ctx.IsAtEnd) break;

                int startPos = ctx.Save();
                var node = ParseSingle(ref ctx);

                if (node != null)
                    root.Statements.Add(node);

                SkipTrivia(ref ctx);

                // Handle semicolons
                if (!ctx.IsAtEnd && (ctx.Peek().Kind == TokenKind.ExpressionTerminator || ctx.Peek().Kind == TokenKind.ExpressionTerminator))
                    ctx.Consume();

                // Safety break for infinite loops
                if (ctx.Save() == startPos && !ctx.IsAtEnd)
                    ctx.Consume();
            }

            return root;
        }

        public static SyntaxNode? ParseSingle(ref ParserContext ctx)
        {
            SkipTrivia(ref ctx);
            if (ctx.IsAtEnd) return null;

            switch (ctx.Peek().Kind)
            {
                case TokenKind.Function:
                    if (Grammar.FunctionDeclaration().TryMatch(ref ctx, out var fn)) return fn;
                    if (Grammar.AnonymousFunction().TryMatch(ref ctx, out var an)) return an;
                    break;
            
                case TokenKind.Return:
                    if (Grammar.Return().TryMatch(ref ctx, out var returnNode)) return returnNode;
                    break;

                case TokenKind.If:
                    if (Grammar.If().TryMatch(ref ctx, out var ifNode)) return ifNode;
                    break;
            
                case TokenKind.While:
                    if (Grammar.WhileExpression().TryMatch(ref ctx, out var whileNode)) return whileNode;
                    break;
            
                case TokenKind.For:
                    if (Grammar.ForExpression().TryMatch(ref ctx, out var forNode)) return forNode;
                    break;
            
                case TokenKind.Foreach:
                    if (Grammar.ForeachExpression().TryMatch(ref ctx, out var foreachNode)) return foreachNode;
                    break;
            }

            // The Climber (This is the only way into the expression tree)
            var climber = new InnerExpressionPattern(0);
            if (climber.TryMatch(ref ctx, out var result)) return result;

            return null;
        }

        public static SyntaxNode? ParseAtom(ref ParserContext ctx)
        {
            SkipTrivia(ref ctx);
            if (ctx.IsAtEnd) return null;

            var token = ctx.Peek();

            // 1. IDENTIFIERS & FUNCTION CALLS (e.g. print(...))
            if (token.Kind == TokenKind.Identifier)
            {
                // Try matching a function call first
                if (Grammar.FunctionCall().TryMatch(ref ctx, out var callNode))
                    return callNode;

                // Fallback to plain identifier if needed
                return new IdentifierNode { Token = ctx.Consume() };
            }

            // 2. ARRAY LITERALS
            if (token.Kind == TokenKind.LeftBracket ||
                token.Kind == TokenKind.Array ||
                (token.Kind == TokenKind.Identifier && token.TextValue(in ctx.Source) == "array"))
            {
                if (Grammar.ArrayLiteral().TryMatch(ref ctx, out var arrayNode))
                    return arrayNode;
            
                throw new Exception($"Unable to parse array {token.TextValue(in ctx.Source)}");
            }

            // 3. VARIABLES (e.g. $nums or $fn())
            if (token.Kind == TokenKind.Variable)
            {
                var varToken = ctx.Consume();
                SkipTrivia(ref ctx);

                // Variable function calls: $fn()
                if (!ctx.IsAtEnd && ctx.Peek().Kind == TokenKind.LeftParen)
                {
                    ctx.Consume();
                    var args = new List<ExpressionNode>();
                    while (!ctx.IsAtEnd && ctx.Peek().Kind != TokenKind.RightParen)
                    {
                        if (new InnerExpressionPattern(0).TryMatch(ref ctx, out var arg))
                            args.Add(arg as ExpressionNode ?? throw new Exception("Invalid arg"));
                    
                        SkipTrivia(ref ctx);
                        if (!ctx.IsAtEnd && ctx.Peek().Kind == TokenKind.Comma) ctx.Consume();
                    }
                    if (!ctx.IsAtEnd && ctx.Peek().Kind == TokenKind.RightParen) ctx.Consume();
                    return new FunctionCallNode { Callee = new VariableNode { Token = varToken }, Args = args };
                }

                return new VariableNode { Token = varToken };
            }

            // 4. LITERALS
            if (token.Kind == TokenKind.IntLiteral || token.Kind == TokenKind.StringLiteral)
            {
                if (Grammar.Literal().TryMatch(ref ctx, out var lit)) return lit;
            }

            // 5. PREFIX OPERATORS
            if (token.Kind == TokenKind.Increment || token.Kind == TokenKind.Decrement)
            {
                var op = ctx.Consume();
                var operand = ParseAtom(ref ctx);
                if (operand != null) return new PrefixExpressionNode(op, operand);
            }

            // 6. PAREN GROUP
            if (token.Kind == TokenKind.LeftParen)
            {
                ctx.Consume();
                var inner = new InnerExpressionPattern(0).TryMatch(ref ctx, out var innerNode) ? innerNode : null;
                SkipTrivia(ref ctx);
                if (!ctx.IsAtEnd && ctx.Peek().Kind == TokenKind.RightParen) ctx.Consume();
                return inner;
            }

            return null;
        }

        public static void SkipTrivia(ref ParserContext ctx)
        {
            while (!ctx.IsAtEnd && (ctx.Peek().Kind == TokenKind.Whitespace || ctx.Peek().Kind == TokenKind.NewLine))
                ctx.Consume();
        }
    }
}
