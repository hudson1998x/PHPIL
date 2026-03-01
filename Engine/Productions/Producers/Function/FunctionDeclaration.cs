using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;
using System;
using System.Collections.Generic;

namespace PHPIL.Engine.Producers;

public class FunctionDeclaration : Production
{
    public FunctionNode? Node { get; private set; }

    public override Producer Init() =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            if (pointer >= tokens.Length || tokens[pointer].Kind != TokenKind.Function)
                return new Match(false, pointer, pointer);

            int current = pointer + 1;
            current = SkipTrivia(tokens, current);

            // Function name
            if (current >= tokens.Length || tokens[current].Kind != TokenKind.Identifier)
                return new Match(false, pointer, pointer);

            var nameToken = tokens[current];
            current++;
            current = SkipTrivia(tokens, current);

            // Opening paren
            if (current >= tokens.Length || tokens[current].Kind != TokenKind.LeftParen)
                return new Match(false, pointer, pointer);

            current++;
            current = SkipTrivia(tokens, current);

            // Parameter list
            var parameters = new List<FunctionParameter>();
            while (current < tokens.Length && tokens[current].Kind != TokenKind.RightParen)
            {
                current = SkipTrivia(tokens, current);

                // Optional type hint
                Token typeHint = default;
                if (tokens[current].Kind == TokenKind.Identifier)
                {
                    typeHint = tokens[current];
                    current++;
                    current = SkipTrivia(tokens, current);
                }

                // Variable name
                if (current >= tokens.Length || tokens[current].Kind != TokenKind.Variable)
                    return new Match(false, pointer, pointer);

                var paramName = tokens[current];
                current++;
                current = SkipTrivia(tokens, current);

                parameters.Add(new FunctionParameter
                {
                    Name = paramName,
                    TypeHint = typeHint
                });

                // Comma between parameters
                if (current < tokens.Length && tokens[current].Kind == TokenKind.Comma)
                {
                    current++;
                    current = SkipTrivia(tokens, current);
                }
            }

            // Closing paren
            if (current >= tokens.Length || tokens[current].Kind != TokenKind.RightParen)
                return new Match(false, pointer, pointer);

            current++;
            current = SkipTrivia(tokens, current);

            // Optional return type
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

            // Body block
            var block = new Block();
            var blockMatch = block.Init()(tokens, source, current);
            if (!blockMatch.Success)
                return new Match(false, pointer, pointer);

            current = blockMatch.End;

            Node = new FunctionNode
            {
                Name = nameToken,
                Params = parameters,
                ReturnType = returnType, // store return type (optional)
                Body = block.Node,
                RangeStart = pointer,
                RangeEnd = current
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