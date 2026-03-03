using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class LookaheadPattern : Pattern
{
    private readonly Pattern _inner;
    private readonly bool _shouldNegate; // True for "NotFollowedBy"

    public LookaheadPattern(Pattern inner, bool negate = false)
    {
        _inner = inner;
        _shouldNegate = negate;
    }

    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        result = null;
        int startPos = ctx.Save();
		
        bool matched = _inner.TryMatch(ref ctx, out _);
		
        // Always restore position - lookahead doesn't consume!
        ctx.Restore(startPos);

        return _shouldNegate ? !matched : matched;
    }
}