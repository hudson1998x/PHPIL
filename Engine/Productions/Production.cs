using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.Productions;

public abstract class Production
{
    public abstract Producer Init();
    public virtual void OnValue() { }

    // ── Primitives ────────────────────────────────────────────────────────────

    protected static Producer Token(TokenKind kind) =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            if (pointer < tokens.Length && tokens[pointer].Kind == kind)
                return new Match(true, pointer, pointer + 1);
            return new Match(false, pointer, pointer);
        };

    protected static Producer AnyToken() =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            if (pointer < tokens.Length)
                return new Match(true, pointer, pointer + 1);
            return new Match(false, pointer, pointer);
        };

    // ── Combinators ───────────────────────────────────────────────────────────

    /// <summary>
    /// All producers must match in order (AND).
    /// Resets pointer if any step fails.
    /// </summary>
    protected static Producer Sequence(params Producer[] producers) =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            int current = pointer;
            foreach (var producer in producers)
            {
                var match = producer(tokens, source, current);
                if (!match.Success) return new Match(false, pointer, pointer);
                current = match.End;
            }
            return new Match(true, pointer, current);
        };

    /// <summary>
    /// Try each producer, return the longest match (OR).
    /// </summary>
    protected static Producer AnyOf(params Producer[] producers) =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            Match best = new Match(false, pointer, pointer);

            foreach (var producer in producers)
            {
                var match = producer(tokens, source, pointer);
                if (match.Success && match.End > best.End)
                    best = match;
            }

            return best;
        };

    /// <summary>
    /// Match zero or one occurrence (optional).
    /// Always succeeds, pointer only advances on match.
    /// </summary>
    protected static Producer Optional(Producer producer) =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            var match = producer(tokens, source, pointer);
            return match.Success ? match : new Match(true, pointer, pointer);
        };

    /// <summary>
    /// Match one or more repetitions (+ in PEG).
    /// Resets if zero matches found.
    /// </summary>
    protected static Producer Repeated(Producer producer) =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            var first = producer(tokens, source, pointer);
            if (!first.Success) return new Match(false, pointer, pointer);

            int current = first.End;
            while (true)
            {
                var match = producer(tokens, source, current);
                if (!match.Success) break;
                current = match.End;
            }

            return new Match(true, pointer, current);
        };

    /// <summary>
    /// Match zero or more repetitions (* in PEG).
    /// Always succeeds.
    /// </summary>
    protected static Producer RepeatedOptional(Producer producer) =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            int current = pointer;
            while (true)
            {
                var match = producer(tokens, source, current);
                if (!match.Success) break;
                current = match.End;
            }

            return new Match(true, pointer, current);
        };

    /// <summary>
    /// Lookahead - matches if producer would match, but does NOT consume tokens.
    /// </summary>
    protected static Producer Peek(Producer producer) =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            var match = producer(tokens, source, pointer);
            return new Match(match.Success, pointer, pointer);
        };

    /// <summary>
    /// Negative lookahead - succeeds if producer would NOT match.
    /// Does NOT consume tokens.
    /// </summary>
    protected static Producer Not(Producer producer) =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            var match = producer(tokens, source, pointer);
            return new Match(!match.Success, pointer, pointer);
        };

    /// <summary>
    /// Lazy reference to another production rule - for recursive grammars.
    /// </summary>
    protected static Producer Ref(Func<Producer> producerFactory) =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
            producerFactory()(tokens, source, pointer);

    /// <summary>Delegate to another Production's Init() rule.</summary>
    protected static Producer Prefab<T>() where T : Production, new() =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
            new T().Init()(tokens, source, pointer);

    /// <summary>
    /// Consume tokens until the closing token is found, optionally tracking nested bracket pairs.
    /// </summary>
    protected static Producer AnyUntil(TokenKind closing, bool trackParens = false, bool trackBraces = false, bool trackBrackets = false) =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            int current = pointer;
            int parenDepth = 0;
            int braceDepth = 0;
            int bracketDepth = 0;

            while (current < tokens.Length)
            {
                var kind = tokens[current].Kind;

                if (trackParens)
                {
                    if (kind == TokenKind.LeftParen)  parenDepth++;
                    if (kind == TokenKind.RightParen) parenDepth--;
                }

                if (trackBraces)
                {
                    if (kind == TokenKind.LeftBrace)  braceDepth++;
                    if (kind == TokenKind.RightBrace) braceDepth--;
                }

                if (trackBrackets)
                {
                    if (kind == TokenKind.LeftBracket)  bracketDepth++;
                    if (kind == TokenKind.RightBracket) bracketDepth--;
                }

                if (kind == closing && parenDepth <= 0 && braceDepth <= 0 && bracketDepth <= 0)
                    return new Match(true, pointer, current + 1);

                current++;
            }

            return new Match(false, pointer, pointer);
        };

    /// <summary>
    /// Wraps a producer — on success, calls onMatch with the matched token range.
    /// </summary>
    protected static Producer Capture(Producer producer, Action<int, int> onMatch) =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            var match = producer(tokens, source, pointer);
            if (match.Success)
                onMatch(match.Start, match.End);
            return match;
        };
}