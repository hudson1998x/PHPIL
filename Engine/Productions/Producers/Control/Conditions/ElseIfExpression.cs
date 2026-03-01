using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Producers;

public class ElseIfExpression : Production
{
    public ElseIfNode? Node { get; private set; }

    public override Producer Init()
    {
        var expr  = new Expression();
        var block = new Block();

        var whitespaceOrNewline = AnyOf(Prefab<Whitespace>(), Prefab<NewLine>());

        return Capture(
            Sequence(
                Token(TokenKind.ElseIf),
                Optional(whitespaceOrNewline),
                expr.Init(),
                Optional(whitespaceOrNewline),
                block.Init()
            ),
            (start, end) => Node = new ElseIfNode
            {
                RangeStart = start,
                RangeEnd   = end,
                Expression = expr.Node,
                Body       = block.Node
            }
        );
    }
}