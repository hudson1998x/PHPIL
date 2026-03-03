using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class ChoicePattern : Pattern
{
    private readonly Pattern[] _choices;
    public ChoicePattern(Pattern[] choices) => _choices = choices;

    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        int startPos = ctx.Save();

        foreach (var choice in _choices)
        {
            if (choice.TryMatch(ref ctx, out result))
            {
                return true;
            }
            ctx.Restore(startPos); // Reset for the next choice
        }

        result = null;
        return false;
    }
}