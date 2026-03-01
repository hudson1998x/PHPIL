using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Producers;

public class KeywordExpressionBlock : Production
{
    public virtual SyntaxNode? Node { get; private set; }

    public override Producer Init()
    {
        var expr  = new Expression();
        var block = new Block();

        var whitespaceOrNewline = AnyOf(
            Prefab<Whitespace>(),
            Prefab<NewLine>()
        );

        return Capture(
            Sequence(
                AnyToken(),                    // the keyword
                Optional(whitespaceOrNewline),
                expr.Init(),                   // runs and populates expr.Node
                Optional(whitespaceOrNewline),
                block.Init()                   // runs and populates block.Node
            ),
            (start, end) =>
            {
                Node = new IfNode
                {
                    RangeStart = start,
                    RangeEnd   = end,
                    Expression = expr.Node,
                    Body       = block.Node
                };
            }
        );
    }
}