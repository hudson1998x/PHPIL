using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class OptionalPattern : Pattern
{
    private readonly Pattern _inner;
    private readonly Action<SyntaxNode>? _callback;

    public OptionalPattern(Pattern inner, Action<SyntaxNode>? callback)
    {
        _inner = inner;
        _callback = callback;
    }

    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        int startPos = ctx.Save();
        if (_inner.TryMatch(ref ctx, out result))
        {
            if (result != null) _callback?.Invoke(result);
        }
        else
        {
            ctx.Restore(startPos);
            result = null; 
        }

        return true; // Optional never fails the parent sequence
    }
}