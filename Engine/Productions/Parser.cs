
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
                if (ctx.Peek().Kind == TokenKind.PhpOpenTag)
                {
                    ctx.Consume();
                    continue;
                }
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
                
                case TokenKind.Variable:
                {
                    // Try variable assignment first
                    if (Grammar.VariableAssignment().TryMatch(ref ctx, out var varNode))
                        return varNode;

                    // If not assignment, try to parse as a simple variable/expression
                    var exprClimber = new InnerExpressionPattern(0);
                    if (exprClimber.TryMatch(ref ctx, out var exprNode))
                        return exprNode;

                    // Nothing worked
                    var variableToken = ctx.Peek();
                    throw new Exception($"Unknown token ({variableToken.Kind}) {variableToken.TextValue(in ctx.Source)}");
                }
                
                case TokenKind.FloatLiteral:
                case TokenKind.IntLiteral:
                case TokenKind.TrueLiteral:
                case TokenKind.FalseLiteral:
                case TokenKind.NullLiteral:
                    var literalValue = ctx.Consume();
                    var literalNode = new LiteralNode() { Token = literalValue, RangeStart = ctx.Position - 1, RangeEnd = ctx.Position };
                    return literalNode;
                
                case TokenKind.Identifier:
                    // Try parsing a function call first
                    if (Grammar.FunctionCall().TryMatch(ref ctx, out var callNode)) 
                        return callNode;

                    // Otherwise, it’s a plain identifier (variable or constant)
                    return new IdentifierNode { Token = ctx.Consume() };
                
                case TokenKind.LeftParen:
                    if (Grammar.Expressions.Outer().TryMatch(ref ctx, out var expressionNode))
                    {
                        return expressionNode;
                    }
                    if (Grammar.Expressions.Inner().TryMatch(ref ctx, out var inner))
                    {
                        return inner;
                    }
                    throw new  Exception($"Unable to parse expression {ctx.Consume()}");
                
                default:
                    var token = ctx.Peek();
                    throw new Exception($"Unknown token ({token.Kind}) {token.TextValue(in ctx.Source)}");
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

            // 1. IDENTIFIERS & FUNCTION CALLS
            if (token.Kind == TokenKind.Identifier)
            {
                // Always try to parse a function call first
                if (Grammar.FunctionCall().TryMatch(ref ctx, out var callNode))
                    return callNode;

                // Otherwise, plain identifier
                return new IdentifierNode { Token = ctx.Consume() };
            }

            // 2. VARIABLES (can also be function calls)
            if (token.Kind == TokenKind.Variable)
            {
                // Always try FunctionCallPattern first
                if (Grammar.FunctionCall().TryMatch(ref ctx, out var varCall))
                    return varCall;

                // Fallback to plain variable
                return new VariableNode { Token = ctx.Consume() };
            }

            // 3. LITERALS
            if (token.Kind is TokenKind.IntLiteral 
                          or TokenKind.StringLiteral 
                          or TokenKind.FloatLiteral 
                          or TokenKind.TrueLiteral 
                          or TokenKind.FalseLiteral)
            {
                if (Grammar.Literal().TryMatch(ref ctx, out var lit))
                    return lit;
            }

            // 4. PREFIX OPERATORS
            if (token.Kind == TokenKind.Increment || token.Kind == TokenKind.Decrement)
            {
                var op = ctx.Consume();
                var operand = ParseAtom(ref ctx);
                if (operand != null)
                    return new PrefixExpressionNode(op, operand);
            }

            // 5. PAREN GROUP
            if (token.Kind == TokenKind.LeftParen)
            {
                ctx.Consume();
                var inner = new InnerExpressionPattern(0).TryMatch(ref ctx, out var innerNode) ? innerNode : null;
                SkipTrivia(ref ctx);
                if (!ctx.IsAtEnd && ctx.Peek().Kind == TokenKind.RightParen) ctx.Consume();
                return inner;
            }

            // 6. ARRAY LITERALS
            if (token.Kind == TokenKind.LeftBracket ||
                token.Kind == TokenKind.Array ||
                (token.Kind == TokenKind.Identifier && token.TextValue(in ctx.Source) == "array"))
            {
                if (Grammar.ArrayLiteral().TryMatch(ref ctx, out var arrayNode))
                    return arrayNode;

                throw new Exception($"Unable to parse array {token.TextValue(in ctx.Source)}");
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
