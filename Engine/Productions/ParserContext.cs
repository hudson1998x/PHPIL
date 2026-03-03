using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.Productions;

public ref struct ParserContext
{
    public ReadOnlySpan<Token> Tokens;
    public ReadOnlySpan<char> Source;
    public int Position;

    public ParserContext(ReadOnlySpan<Token> tokens, ReadOnlySpan<char> source)
    {
        Tokens = tokens;
        Source = source;
        Position = 0;
    }

    public Token Peek(int offset = 0) => 
        (Position + offset < Tokens.Length) ? Tokens[Position + offset] : default;

    public Token Consume() => Tokens[Position++];

    public bool IsAtEnd => Position >= Tokens.Length;

    // Captures the current state for backtracking
    public int Save() => Position;
    public void Restore(int pos) => Position = pos;

    public ReadOnlySpan<char> GetLexeme(Token t) => Source.Slice(t.RangeStart, t.RangeEnd - t.RangeStart);
}