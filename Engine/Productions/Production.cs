using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.Productions;

/// <summary>
/// Base class for all production rules in the parser combinator system.
/// Subclasses define a grammar rule by implementing <see cref="Init"/>, which
/// composes the protected combinator helpers into a single <see cref="Producer"/> delegate.
/// The class itself is stateless at this level — any captured parse results live
/// in the concrete subclass (typically exposed via a <c>Node</c> property).
///
/// <para>
/// The overall design is a hand-written PEG (Parsing Expression Grammar) combinator.
/// Rather than generating a parser from a grammar file, rules are expressed as
/// ordinary C# objects that compose <see cref="Producer"/> delegates. This keeps
/// the grammar and the AST-building logic in the same place, makes debugging
/// straightforward (just set a breakpoint inside a lambda), and avoids a
/// code-generation step in the build pipeline.
/// </para>
/// </summary>
public abstract class Production
{
    /// <summary>
    /// Build and return the <see cref="Producer"/> delegate that represents this
    /// production rule. Called once per parse attempt; the returned delegate is
    /// then invoked against the token stream.
    /// </summary>
    public abstract Producer Init();

    /// <summary>
    /// Optional hook fired when the production yields a meaningful value.
    /// Override in concrete productions to act on a successful match
    /// (e.g. constructing a node from captured token ranges).
    /// The default implementation is a no-op.
    /// </summary>
    public virtual void OnValue() { }

    // ── Primitives ────────────────────────────────────────────────────────────
    // The smallest possible matching units — a single token or any token.
    // All higher-level combinators are built on top of these two.

    /// <summary>
    /// Matches exactly one token of the specified <paramref name="kind"/>.
    /// Advances the pointer by one on success; leaves it unchanged on failure.
    /// This is the atomic unit that almost every grammar rule bottoms out into.
    /// </summary>
    protected static Producer Token(TokenKind kind) =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            if (pointer < tokens.Length && tokens[pointer].Kind == kind)
                return new Match(true, pointer, pointer + 1);
            return new Match(false, pointer, pointer);
        };

    /// <summary>
    /// Matches any single token, regardless of kind. Useful as a wildcard inside
    /// sequences where the token's identity doesn't matter, only its presence does —
    /// for example, consuming the body of a construct that's being parsed opaquely,
    /// or skipping over tokens inside an <see cref="AnyUntil"/> alternative.
    /// Fails only at end-of-stream.
    /// </summary>
    protected static Producer AnyToken() =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            if (pointer < tokens.Length)
                return new Match(true, pointer, pointer + 1);
            return new Match(false, pointer, pointer);
        };

    // ── Combinators ───────────────────────────────────────────────────────────
    // Higher-order functions that take one or more Producers and return a new
    // Producer, allowing grammar rules to be composed declaratively.
    //
    // If you're familiar with regex, the mapping is roughly:
    //   Sequence        → concatenation  (a b c)
    //   AnyOf           → alternation    (a | b | c)
    //   Optional        → ?
    //   Repeated        → +
    //   RepeatedOptional→ *
    //   Peek            → (?=...)  positive lookahead
    //   Not             → (?!...)  negative lookahead

    /// <summary>
    /// Logical AND — every producer in <paramref name="producers"/> must match
    /// consecutively. Matching is greedy and left-to-right; the pointer advances
    /// through the stream as each step succeeds. If any step fails the whole
    /// sequence fails and the pointer is reset to where it started, leaving the
    /// stream untouched (PEG-style backtracking).
    ///
    /// <para>
    /// The reset-on-failure behaviour is important for correctness: without it, a
    /// partially-matched sequence would leave the pointer mid-stream, causing
    /// everything after it to parse from the wrong position.
    /// </para>
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
    /// Logical OR — tries each producer at the same starting position and returns
    /// the one that consumed the most tokens (longest match wins). This greedy
    /// disambiguation avoids the ambiguity of a first-match-wins strategy when
    /// multiple alternatives could match at the same position — for example,
    /// <c>+=</c> vs <c>+</c> where a first-match rule would always pick the shorter
    /// one if it appears first in the list.
    /// Fails only if no producer matched at all.
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
    /// Equivalent to <c>?</c> in PEG / regex — zero or one occurrence.
    /// Always succeeds: if the inner producer matches, the pointer advances;
    /// if it doesn't, the pointer stays put and success is still reported.
    /// This makes it safe to use inside a <see cref="Sequence"/> for truly optional
    /// parts of a construct (e.g. a trailing comma, a type hint, a default value)
    /// without causing the whole sequence to fail when they're absent.
    /// </summary>
    protected static Producer Optional(Producer producer) =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            var match = producer(tokens, source, pointer);
            return match.Success ? match : new Match(true, pointer, pointer);
        };

    /// <summary>
    /// Equivalent to <c>+</c> in PEG / regex — one or more occurrences.
    /// Requires at least one successful match; keeps consuming as long as the
    /// producer keeps succeeding. Resets and fails if the very first attempt fails,
    /// ensuring the caller always gets a non-empty match or nothing at all.
    ///
    /// <para>
    /// The "require at least one" distinction from <see cref="RepeatedOptional"/>
    /// matters when you want to assert that a list-like construct must be
    /// non-empty — e.g. a function's parameter list must have at least one parameter
    /// if the parens are non-empty.
    /// </para>
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
    /// Equivalent to <c>*</c> in PEG / regex — zero or more occurrences.
    /// Like <see cref="Repeated"/> but always succeeds, even if the inner producer
    /// never matches. Use this when the construct is genuinely optional in quantity,
    /// such as the statements inside a block body — a block with no statements is
    /// perfectly valid and shouldn't cause a parse failure.
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
    /// Positive lookahead — reports whether the inner producer would match at the
    /// current position, but crucially does <em>not</em> advance the pointer.
    /// Use this to assert that something <em>must</em> follow without consuming it,
    /// so a subsequent combinator in a <see cref="Sequence"/> can handle it properly.
    ///
    /// <para>
    /// For example, you might <c>Peek</c> for a <c>:</c> to confirm you're looking
    /// at a ternary expression before committing to that branch of an <see cref="AnyOf"/>,
    /// without eating the <c>:</c> that the ternary rule itself needs to consume.
    /// </para>
    /// </summary>
    protected static Producer Peek(Producer producer) =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            var match = producer(tokens, source, pointer);
            return new Match(match.Success, pointer, pointer);
        };

    /// <summary>
    /// Negative lookahead — succeeds only if the inner producer would <em>fail</em>.
    /// Like <see cref="Peek"/>, no tokens are consumed either way.
    ///
    /// <para>
    /// Useful for ruling out ambiguous continuations. A classic PHP example:
    /// distinguishing a plain variable read (<c>$x</c>) from a function-call-on-variable
    /// (<c>$x()</c>) — you can <c>Not(Token(LeftParen))</c> after matching the variable
    /// to ensure you only take the "plain read" branch when no call follows.
    /// </para>
    /// </summary>
    protected static Producer Not(Producer producer) =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            var match = producer(tokens, source, pointer);
            return new Match(!match.Success, pointer, pointer);
        };

    /// <summary>
    /// Deferred / lazy reference to another production rule. The
    /// <paramref name="producerFactory"/> is only invoked when the returned
    /// <see cref="Producer"/> is actually executed, breaking the circular reference
    /// that would otherwise arise when a grammar rule refers to itself (directly or
    /// indirectly).
    ///
    /// <para>
    /// Without this, recursive rules like expressions containing sub-expressions
    /// would cause a <see cref="StackOverflowException"/> at rule-construction time,
    /// because building the outer rule would immediately try to build the inner rule,
    /// which tries to build the outer rule again, and so on. <see cref="Ref"/> defers
    /// that construction until parse time, by which point the call stack is bounded by
    /// the actual nesting depth of the source code rather than infinite recursion.
    /// </para>
    /// </summary>
    protected static Producer Ref(Func<Producer> producerFactory) =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
            producerFactory()(tokens, source, pointer);

    /// <summary>
    /// Instantiates another <see cref="Production"/> subclass and delegates to its
    /// <see cref="Init"/> rule. A convenience shorthand for reusing a fully-defined
    /// named production inside a larger rule without wiring it up manually.
    ///
    /// <para>
    /// Prefer this over inlining the same combinator chain in multiple places —
    /// it keeps rules DRY and means a fix to the shared production automatically
    /// propagates everywhere it's referenced.
    /// </para>
    /// </summary>
    protected static Producer Prefab<T>() where T : Production, new() =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
            new T().Init()(tokens, source, pointer);

    /// <summary>
    /// Consumes tokens greedily until the <paramref name="closing"/> token is
    /// encountered at depth zero. The optional tracking flags tell the combinator
    /// to monitor nested bracket pairs of the corresponding kind, so an inner closing
    /// token that belongs to a nested structure isn't mistaken for the real end.
    ///
    /// <para>
    /// For example, when scanning for the <c>)</c> that closes a function argument
    /// list, enabling <paramref name="trackParens"/> prevents an early exit on a
    /// <c>)</c> that closes an inner sub-expression like <c>foo(bar(1), 2)</c>.
    /// You can combine flags freely — e.g. enable all three when consuming an
    /// arbitrary expression that may contain any mix of grouping tokens.
    /// </para>
    ///
    /// <para>
    /// The closing token itself is included in the consumed range, so the caller's
    /// pointer lands just past it and doesn't need to manually skip the delimiter.
    /// Fails if end-of-stream is reached before the closing token is found —
    /// this surfaces unclosed brackets as a parse failure rather than silently
    /// producing a malformed node.
    /// </para>
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

                // Only treat this token as the real closing delimiter if we're back
                // at the top level — all tracked nested structures must be balanced first.
                if (kind == closing && parenDepth <= 0 && braceDepth <= 0 && bracketDepth <= 0)
                    return new Match(true, pointer, current + 1);

                current++;
            }

            // Reached end-of-stream without finding a balanced closing token.
            return new Match(false, pointer, pointer);
        };

    /// <summary>
    /// Side-effect wrapper — runs <paramref name="producer"/> normally, and on
    /// success fires <paramref name="onMatch"/> with the start and end token indices
    /// of the matched range. The match result itself is passed through unchanged,
    /// so <see cref="Capture"/> is transparent to any combinator wrapping it.
    ///
    /// <para>
    /// This is the primary mechanism for extracting parsed data out of a combinator
    /// pipeline. Concrete productions pass lambdas that write into their own fields,
    /// meaning AST node construction happens incrementally as each named sub-rule
    /// fires, rather than requiring a second pass over the token range after the
    /// top-level match completes. Keeping the capture co-located with the rule that
    /// defines what's being captured also makes the grammar much easier to follow.
    /// </para>
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