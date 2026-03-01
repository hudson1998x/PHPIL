using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.Productions;

// Core delegate - all combinators share this signature
public readonly record struct Match(bool Success, int Start, int End);

public delegate Match Producer(
    in ReadOnlySpan<Token> tokens,
    in ReadOnlySpan<char> source,
    int pointer
);