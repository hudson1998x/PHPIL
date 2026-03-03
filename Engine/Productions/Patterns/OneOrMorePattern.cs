using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class OneOrMorePattern : Pattern
{
    private readonly Pattern _inner;
    private readonly Action<SyntaxNode>? _callback;

    public OneOrMorePattern(Pattern inner, Action<SyntaxNode>? callback)
    {
        _inner = inner;
        _callback = callback;
    }

    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        int startPos = ctx.Save();
        result = null;

        // First match is mandatory
        if (!_inner.TryMatch(ref ctx, out var firstMatch))
        {
            ctx.Restore(startPos);
            return false;
        }
		
        _callback?.Invoke(firstMatch!);

        // Subsequent matches are optional
        while (!ctx.IsAtEnd)
        {
            int loopStart = ctx.Save();
            if (_inner.TryMatch(ref ctx, out var nextMatch))
            {
                _callback?.Invoke(nextMatch!);
            }
            else
            {
                ctx.Restore(loopStart);
                break;
            }
        }

        return true;
    }
}