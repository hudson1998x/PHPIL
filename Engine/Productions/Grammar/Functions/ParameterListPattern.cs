using System.Collections.Generic;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class ParameterListPattern : Pattern
{
    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        var parameters = new List<FunctionParameter>();
        int start = ctx.Save();
        result = null;

        // 1. Open Paren
        if (ctx.Peek().Kind != TokenKind.LeftParen) return false;
        ctx.Consume();
        SkipTrivia(ref ctx);

        // 2. Parameter Loop
        while (!ctx.IsAtEnd && ctx.Peek().Kind != TokenKind.RightParen)
        {
            Token? typeHint = null;
            SyntaxNode? defaultValue = null;

            // Check for Type Hint: Identifier or array keyword followed by a Variable
            if ((ctx.Peek().Kind == TokenKind.Identifier || ctx.Peek().Kind == TokenKind.Array) 
                && PeekNextNonTrivia(ref ctx).Kind == TokenKind.Variable)
            {
                typeHint = ctx.Consume();
                SkipTrivia(ref ctx);
            }

            // Must have a Variable name
            if (ctx.Peek().Kind != TokenKind.Variable)
            {
                ctx.Restore(start);
                return false;
            }

            // Must have a Variable name
            if (ctx.Peek().Kind != TokenKind.Variable)
            {
                ctx.Restore(start);
                return false;
            }

            // Must have a Variable name
            if (ctx.Peek().Kind != TokenKind.Variable)
            {
                ctx.Restore(start);
                return false;
            }

            var nameToken = ctx.Consume();
            SkipTrivia(ref ctx);

            // Optional default value
            if (ctx.Peek().Kind == TokenKind.AssignEquals)
            {
                ctx.Consume();
                SkipTrivia(ref ctx);

                if (!Grammar.Expressions.Inner().TryMatch(ref ctx, out var exprNode))
                {
                    ctx.Restore(start);
                    return false;
                }

                defaultValue = exprNode;
                SkipTrivia(ref ctx);
            }

            parameters.Add(new FunctionParameter
            {
                TypeHint = typeHint,
                Name = nameToken,
                DefaultValue = defaultValue
            });

            // Handle Comma
            if (ctx.Peek().Kind == TokenKind.Comma)
            {
                ctx.Consume();
                SkipTrivia(ref ctx);

                // Allow trailing comma
                if (ctx.Peek().Kind == TokenKind.RightParen) break;
            }
            else if (ctx.Peek().Kind != TokenKind.RightParen)
            {
                ctx.Restore(start);
                return false;
            }
        }

        // 3. Close Paren
        if (ctx.Peek().Kind != TokenKind.RightParen)
        {
            ctx.Restore(start);
            return false;
        }

        ctx.Consume();

        result = new ParameterListNode { Parameters = parameters };
        return true;
    }

    private void SkipTrivia(ref ParserContext ctx)
    {
        while (!ctx.IsAtEnd && (ctx.Peek().Kind == TokenKind.Whitespace || ctx.Peek().Kind == TokenKind.NewLine))
        {
            ctx.Consume();
        }
    }

    private Token PeekNextNonTrivia(ref ParserContext ctx)
    {
        for (int i = 1; ; i++)
        {
            if (ctx.Position + i >= ctx.Tokens.Length) return default;

            var tok = ctx.Peek(i);
            if (tok.Kind is not (TokenKind.Whitespace or TokenKind.NewLine))
                return tok;
        }
    }
}

public class ParameterListNode : SyntaxNode
{
    public List<FunctionParameter> Parameters { get; set; }
}