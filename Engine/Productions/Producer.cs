using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.Productions;

/// <summary>
/// The result of running a <see cref="Producer"/> against the token stream.
/// Immutable and allocation-free — passed back by value so the combinator
/// pipeline never touches the heap.
/// </summary>
/// <param name="Success">Whether the production matched.</param>
/// <param name="Start">Index of the first token the production consumed.</param>
/// <param name="End">Index of the first token <em>after</em> the match — i.e. where
/// the next production should begin reading. Meaningless if <see cref="Success"/> is false.</param>
public readonly record struct Match(bool Success, int Start, int End);

/// <summary>
/// The fundamental unit of the parser combinator system. Every production rule —
/// from a single token check to a full if-expression — is expressed as a
/// <see cref="Producer"/> delegate, meaning rules compose naturally: a combinator
/// just takes and returns <see cref="Producer"/> instances.
/// </summary>
/// <param name="tokens">The full token stream. Passed as <c>in</c> to avoid copying the span.</param>
/// <param name="source">The raw source text, available for productions that need to
/// extract a token's actual string value (identifiers, literals, etc.).</param>
/// <param name="pointer">The token index at which this production should attempt to match.</param>
/// <returns>A <see cref="Match"/> describing whether the attempt succeeded and,
/// if so, the range of tokens that were consumed.</returns>
public delegate Match Producer(
    in ReadOnlySpan<Token> tokens,
    in ReadOnlySpan<char> source,
    int pointer
);