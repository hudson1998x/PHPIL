using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions;

public abstract class Pattern
{
    public abstract bool TryMatch(ref ParserContext ctx, out SyntaxNode? result);

    #region Combinator Factories

    // Matches a sequence of patterns in order
    public static Pattern Sequence(params (Pattern pattern, Action<SyntaxNode>? callback)[] steps) 
        => new SequencePattern(steps);

    // Tries patterns in order, returns the first that succeeds
    public static Pattern OneOf(params Pattern[] choices) 
		=> new ChoicePattern(choices);

    // Always succeeds; if the inner pattern fails, it returns a null node but doesn't backtrack
    public static Pattern Optional(Pattern pattern, Action<SyntaxNode>? callback = null) 
		=> new OptionalPattern(pattern, callback);

    // Greedily matches a pattern until it fails (useful for argument lists or blocks)
    public static Pattern ZeroOrMore(Pattern pattern, Action<SyntaxNode>? callback = null) 
		=> new RepeatingPattern(pattern, callback);

    // Simple wrapper for a single TokenKind
    public static Pattern MatchToken(TokenKind kind, Func<Token, SyntaxNode> callback) 
		=> new TokenPattern(kind, callback);

    /// <summary>
    /// Matches the pattern one or more times. Fails if the first match fails.
    /// </summary>
    public static Pattern OneOrMore(Pattern pattern, Action<SyntaxNode>? callback = null)
		=> new OneOrMorePattern(pattern, callback);

    /// <summary>
    /// Matches a list of items separated by a specific pattern (e.g., Comma).
    /// </summary>
    public static Pattern SeparatedBy(Pattern item, Pattern separator, Action<SyntaxNode>? callback = null)
		=> new SeparatedByPattern(item, separator, callback);

    /// <summary>
    /// Validates that a pattern matches without consuming any tokens.
    /// </summary>
    public static Pattern Peek(Pattern pattern)
		=> new LookaheadPattern(pattern, negate: false);

    /// <summary>
    /// Validates that a pattern does NOT match. Does not consume tokens.
    /// </summary>
    public static Pattern Not(Pattern pattern)
		=> new LookaheadPattern(pattern, negate: true);

    /// <summary>
    /// Useful for recursive grammars. Defers pattern evaluation until runtime 
    /// to avoid StackOverflow during static initialization.
    /// </summary>
    public static Pattern Recursive(Func<Pattern> factory)
		=> new RecursivePattern(factory);

    /// <summary>
    /// Fails if the parser is not at the end of the token stream.
    /// </summary>
    public static Pattern EOF()
		=> new EofPattern();

    #endregion
}