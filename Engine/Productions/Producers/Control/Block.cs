using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Producers;

public class Block : Production
{
    public BlockNode? Node { get; private set; }

    public override Producer Init() =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            if (pointer >= tokens.Length || tokens[pointer].Kind != TokenKind.LeftBrace)
                return new Match(false, pointer, pointer);

            int current = pointer + 1;
            var body    = new BlockNode { RangeStart = pointer };

            while (current < tokens.Length)
            {
                // Skip trivia
                if (tokens[current].Kind is TokenKind.Whitespace or TokenKind.NewLine
                    or TokenKind.SingleLineComment or TokenKind.MultiLineComment
                    or TokenKind.ExpressionTerminator)
                {
                    current++;
                    continue;
                }

                // End of block
                if (tokens[current].Kind == TokenKind.RightBrace)
                {
                    current++;
                    break;
                }

                // Recursively parse statements inside the block
                var node = Parser.TryProduce(in tokens, in source, tokens[current].Kind, ref current);
                if (node is not null)
                    body.Statements.Add(node);
                else
                    current++; // skip unrecognised token
            }

            body.RangeEnd = current;
            Node          = body;

            return new Match(true, pointer, current);
        };
}