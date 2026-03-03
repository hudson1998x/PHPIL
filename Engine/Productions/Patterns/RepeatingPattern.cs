using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class RepeatingPattern : Pattern
{
    private readonly Pattern _inner;
    private readonly Action<SyntaxNode>? _callback;

    public RepeatingPattern(Pattern inner, Action<SyntaxNode>? callback)
    {
        _inner = inner;
        _callback = callback;
    }

    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        result = null; // Repeating patterns usually feed a list via callbacks
		
        while (!ctx.IsAtEnd)
        {
            int startPos = ctx.Save();
            if (_inner.TryMatch(ref ctx, out var matchedNode))
            {
                if (matchedNode != null) _callback?.Invoke(matchedNode);
            }
            else
            {
                ctx.Restore(startPos);
                break; // Stop repeating when match fails
            }
        }

        return true; // Zero matches is still a success
    }
}