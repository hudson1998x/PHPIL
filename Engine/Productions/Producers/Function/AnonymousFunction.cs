using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Producers;

public class AnonymousFunction : Production
{
    /// <summary>
    /// The AST node produced on a successful match.
    /// <c>null</c> until <see cref="Init"/> fires successfully.
    /// </summary>
    public AnonymousFunctionNode? Node { get; private set; }

    public override Producer Init() =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            // Fast-path rejection — must open with the `function` keyword.
            if (pointer >= tokens.Length || tokens[pointer].Kind != TokenKind.Function)
                return new Match(false, pointer, pointer);

            int current = pointer + 1;
            current = SkipTrivia(tokens, current);

            // Unlike named functions, anonymous functions have no identifier here.
            // If we see one, this is a named declaration — bail out so FunctionDeclaration
            // can handle it instead.
            if (current < tokens.Length && tokens[current].Kind == TokenKind.Identifier)
                return new Match(false, pointer, pointer);

            // Opening paren — mandatory.
            if (current >= tokens.Length || tokens[current].Kind != TokenKind.LeftParen)
                return new Match(false, pointer, pointer);

            current++;
            current = SkipTrivia(tokens, current);

            // Parameter list — same logic as FunctionDeclaration.
            var parameters = new List<FunctionParameter>();
            while (current < tokens.Length && tokens[current].Kind != TokenKind.RightParen)
            {
                current = SkipTrivia(tokens, current);

                // Optional type hint.
                Token typeHint = default;
                if (tokens[current].Kind == TokenKind.Identifier)
                {
                    typeHint = tokens[current];
                    current++;
                    current = SkipTrivia(tokens, current);
                }

                // Variable name is required.
                if (current >= tokens.Length || tokens[current].Kind != TokenKind.Variable)
                    return new Match(false, pointer, pointer);

                var paramName = tokens[current];
                current++;
                current = SkipTrivia(tokens, current);

                parameters.Add(new FunctionParameter
                {
                    Name     = paramName,
                    TypeHint = typeHint
                });

                // Consume optional comma separator.
                if (current < tokens.Length && tokens[current].Kind == TokenKind.Comma)
                {
                    current++;
                    current = SkipTrivia(tokens, current);
                }
            }

            // Closing paren.
            if (current >= tokens.Length || tokens[current].Kind != TokenKind.RightParen)
                return new Match(false, pointer, pointer);

            current++;
            current = SkipTrivia(tokens, current);

            // Optional `use (...)` clause — captures variables from the enclosing scope.
            // e.g. `function() use ($x, &$y) { ... }`
            var useCaptures = new List<UseCapture>();
            if (current < tokens.Length && tokens[current].Kind == TokenKind.Use)
            {
                current++;
                current = SkipTrivia(tokens, current);

                if (current >= tokens.Length || tokens[current].Kind != TokenKind.LeftParen)
                    return new Match(false, pointer, pointer);

                current++;
                current = SkipTrivia(tokens, current);

                while (current < tokens.Length && tokens[current].Kind != TokenKind.RightParen)
                {
                    current = SkipTrivia(tokens, current);

                    // Captures may be by reference: `&$var`
                    bool byRef = false;
                    if (current < tokens.Length && tokens[current].Kind == TokenKind.Ampersand)
                    {
                        byRef = true;
                        current++;
                        current = SkipTrivia(tokens, current);
                    }

                    if (current >= tokens.Length || tokens[current].Kind != TokenKind.Variable)
                        return new Match(false, pointer, pointer);

                    useCaptures.Add(new UseCapture
                    {
                        Name  = tokens[current],
                        ByRef = byRef
                    });

                    current++;
                    current = SkipTrivia(tokens, current);

                    if (current < tokens.Length && tokens[current].Kind == TokenKind.Comma)
                    {
                        current++;
                        current = SkipTrivia(tokens, current);
                    }
                }

                if (current >= tokens.Length || tokens[current].Kind != TokenKind.RightParen)
                    return new Match(false, pointer, pointer);

                current++;
                current = SkipTrivia(tokens, current);
            }

            // Optional return type annotation — `: TypeName`
            Token returnType = default;
            if (current < tokens.Length && tokens[current].Kind == TokenKind.Colon)
            {
                current++;
                current = SkipTrivia(tokens, current);

                if (current < tokens.Length && tokens[current].Kind == TokenKind.Identifier)
                {
                    returnType = tokens[current];
                    current++;
                    current = SkipTrivia(tokens, current);
                }
            }

            // Body block — delegate to Block production.
            var block = new Block();
            var blockMatch = block.Init()(tokens, source, current);
            if (!blockMatch.Success)
                return new Match(false, pointer, pointer);

            current = blockMatch.End;

            Node = new AnonymousFunctionNode
            {
                Params      = parameters,
                UseCaptures = useCaptures,
                ReturnType  = returnType,
                Body        = block.Node,
                RangeStart  = pointer,
                RangeEnd    = current
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