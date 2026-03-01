using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;
using System;

namespace PHPIL.Engine.Producers;

public partial class ReturnStatement : Production
{
    public ReturnNode? Node { get; private set; }

    public override Producer Init() =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            if (pointer >= tokens.Length || tokens[pointer].Kind != TokenKind.Return)
                return new Match(false, pointer, pointer);

            int current = pointer + 1;
            current = SkipTrivia(tokens, current);

            Expression? expression = null;

            // Optional return expression
            var expr = new Expression();
            var exprMatch = expr.Init()(tokens, source, current);
            if (exprMatch.Success)
            {
                expression = expr;
                current = exprMatch.End;
            }

            // Optional semicolon
            current = SkipTrivia(tokens, current);
            if (current < tokens.Length && tokens[current].Kind == TokenKind.ExpressionTerminator)
                current++;

            Node = new ReturnNode
            {
                Expression = expression?.Node,
                RangeStart = pointer,
                RangeEnd   = current
            };

            return new Match(true, pointer, current);
        };

    private static int SkipTrivia(in ReadOnlySpan<Token> tokens, int pointer)
    {
        while (pointer < tokens.Length &&
               (tokens[pointer].Kind == TokenKind.Whitespace || tokens[pointer].Kind == TokenKind.NewLine))
        {
            pointer++;
        }

        return pointer;
    }
}