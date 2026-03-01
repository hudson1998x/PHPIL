using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Producers;

public class VariableAssignment : Production
{
    public VariableDeclaration? Node { get; private set; }

    public override Producer Init() =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            if (pointer >= tokens.Length || tokens[pointer].Kind != TokenKind.Variable)
                return new Match(false, pointer, pointer);

            int current = pointer;

            // Variable name
            var variableToken = tokens[current];
            current++;
            current = SkipTrivia(tokens, current);

            // Expect assignment operator (=)
            if (current >= tokens.Length || tokens[current].Kind != TokenKind.AssignEquals)
                return new Match(false, pointer, pointer);

            current++;
            current = SkipTrivia(tokens, current);

            // Parse expression
            var expression = new Expression();
            var exprMatch  = expression.Init()(tokens, source, current);

            if (!exprMatch.Success)
                return new Match(false, pointer, pointer);

            current = exprMatch.End;
            current = SkipTrivia(tokens, current);

            // Optional semicolon
            if (current < tokens.Length && tokens[current].Kind == TokenKind.ExpressionTerminator)
                current++;

            Node = new VariableDeclaration()
            {
                VariableName = variableToken,
                VariableValue = expression.Node,
                RangeStart = pointer,
                RangeEnd   = current
            };

            return new Match(true, pointer, current);
        };

    private static int SkipTrivia(in ReadOnlySpan<Token> tokens, int pointer)
    {
        while (pointer < tokens.Length &&
               tokens[pointer].Kind is TokenKind.Whitespace or TokenKind.NewLine)
        {
            pointer++;
        }

        return pointer;
    }
}