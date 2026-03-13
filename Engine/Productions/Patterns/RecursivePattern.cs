using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

/// <summary>
/// Defers the evaluation of a pattern until it is actually needed.
/// Essential for mutually recursive rules (e.g., Statement -> Block -> Statement).
/// </summary>
public class RecursivePattern : Pattern
{
    private readonly Func<Pattern> _factory;
    private Pattern? _cachedPattern;

    public RecursivePattern(Func<Pattern> factory)
    {
        _factory = factory;
    }

    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        // Initialize the real pattern only once on first use
        _cachedPattern ??= _factory();
		
        int startPos = ctx.Position;
        bool matched = _cachedPattern.TryMatch(ref ctx, out result);
        
        if (!matched)
        {
            ctx.RecordFailure(startPos, "RecursivePattern", "unknown");
        }
        
        return matched;
    }
}