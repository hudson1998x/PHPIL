using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class SeparatedByPattern : Pattern
{
    private readonly Pattern _item;
    private readonly Pattern _separator;
    private readonly Action<SyntaxNode>? _callback;

    public SeparatedByPattern(Pattern item, Pattern separator, Action<SyntaxNode>? callback)
    {
        _item = item;
        _separator = separator;
        _callback = callback;
    }

    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        result = null;
        int startPos = ctx.Save();

        // Match first item
        if (!_item.TryMatch(ref ctx, out var itemNode))
        {
            // If no items, that's fine for Zero-or-More semantics
            return true; 
        }

        _callback?.Invoke(itemNode!);

        // Match (Separator + Item) pairs
        while (!ctx.IsAtEnd)
        {
            int stepStart = ctx.Save();
			
            // Try separator
            if (!_separator.TryMatch(ref ctx, out _))
            {
                ctx.Restore(stepStart);
                break;
            }

            // Try next item
            if (_item.TryMatch(ref ctx, out var nextItem))
            {
                _callback?.Invoke(nextItem!);
            }
            else
            {
                // Found separator but no item (e.g., "1, ")
                // Backtrack the separator and stop
                ctx.Restore(stepStart);
                break;
            }
        }

        return true;
    }
}