using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Producers;

public class KeywordExpressionBlock : Production
{
    public virtual SyntaxNode? Node { get; private set; }

    public virtual SyntaxNode CreateNode(int start, int end, ExpressionNode? expr, BlockNode? body)
    {
        return new SyntaxNode();
    }

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
                Node = CreateNode(start, end, expr?.Node, block?.Node);
            }
        );
    }
}