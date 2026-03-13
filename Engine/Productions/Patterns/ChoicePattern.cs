using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class ChoicePattern : Pattern
{
    private readonly Pattern[] _choices;
    public ChoicePattern(Pattern[] choices) => _choices = choices;

    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        int startPos = ctx.Save();
        int maxPosition = startPos;

        foreach (var choice in _choices)
        {
            if (choice.TryMatch(ref ctx, out result))
            {
                return true;
            }
            // Track the longest match
            if (ctx.Position > maxPosition)
            {
                maxPosition = ctx.Position;
            }
            ctx.Restore(startPos); // Reset for the next choice
        }

        // Record failure at the longest position
        var currentToken = ctx.Peek();
        ctx.RecordFailure(maxPosition, "choice", $"one of {string.Join(", ", _choices.Select(c => c.GetType().Name))}");
        
        result = null;
        return false;
    }
}