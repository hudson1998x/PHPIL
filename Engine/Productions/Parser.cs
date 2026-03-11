
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
            
                case TokenKind.Namespace:
                    if (Grammar.NamespaceDeclaration().TryMatch(ref ctx, out var ns)) return ns;
                    break;

                case TokenKind.Use:
                    if (Grammar.Use().TryMatch(ref ctx, out var use)) return use;
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
                    if (Grammar.VariableAssignment().TryMatch(ref ctx, out var varNode))
                        return varNode;

                    var exprClimber = new InnerExpressionPattern(0);
                    if (exprClimber.TryMatch(ref ctx, out var exprNode))
                        return exprNode;

                    var variableToken = ctx.Peek();
                    throw new Exception($"Unknown token ({variableToken.Kind}) {variableToken.TextValue(in ctx.Source)}");
                }

                case TokenKind.FloatLiteral:
                case TokenKind.IntLiteral:
                case TokenKind.TrueLiteral:
                case TokenKind.FalseLiteral:
                case TokenKind.NullLiteral:
                case TokenKind.StringLiteral:
                case TokenKind.Identifier:
                case TokenKind.LeftParen:
                case TokenKind.Not:
                case TokenKind.Increment:
                case TokenKind.Decrement:
                {
                    var climber = new InnerExpressionPattern(0);
                    if (climber.TryMatch(ref ctx, out var expr)) return expr;
                    break;
                }

                case TokenKind.Break:
                    if (Grammar.Break().TryMatch(ref ctx, out var breakNode)) return breakNode;
                    break;
                case TokenKind.Continue:
                    if (Grammar.Continue().TryMatch(ref ctx, out var continueNode)) return continueNode;
                    break;
                
                case TokenKind.LeftBrace:
                    if (Grammar.Block().TryMatch(ref ctx, out var blockNode)) return blockNode;
                    break;

                case TokenKind.ExpressionTerminator:
                    return null;
            }

            // Fallback for any other expression-starting tokens
            var finalClimber = new InnerExpressionPattern(0);
            if (finalClimber.TryMatch(ref ctx, out var result)) return result;

            var t = ctx.Peek();
            throw new Exception($"Unknown token ({t.Kind}) {t.TextValue(in ctx.Source)}");
        }

        public static SyntaxNode? ParseAtom(ref ParserContext ctx)
        {
            SkipTrivia(ref ctx);
            if (ctx.IsAtEnd) return null;

            var token = ctx.Peek();

            // 1. IDENTIFIERS & FUNCTION CALLS
            if (token.Kind == TokenKind.Identifier || token.Kind == TokenKind.NamespaceSeparator)
            {
                // Always try to parse a function call first
                if (Grammar.FunctionCall().TryMatch(ref ctx, out var callNode))
                    return callNode;

                // Otherwise, try a qualified name (or plain identifier)
                if (Grammar.QualifiedName().TryMatch(ref ctx, out var qname))
                    return qname;
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

            // 4b. UNARY +/- OPERATORS
            if (token.Kind == TokenKind.Add || token.Kind == TokenKind.Subtract || token.Kind == TokenKind.Not)
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
