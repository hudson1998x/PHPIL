using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Producers;

public class ElseExpression : Production
{
    public BlockNode? Node { get; private set; }

    public override Producer Init()
    {
        var block = new Block();
        var whitespaceOrNewline = AnyOf(Prefab<Whitespace>(), Prefab<NewLine>());

        return Capture(
            Sequence(
                Token(TokenKind.Else),
                Optional(whitespaceOrNewline),
                block.Init()
            ),
            (start, end) =>
            {
                if (block.Node is null)
                {
                    throw new InvalidOperationException("Expected a block, received null");
                }
                Node = block.Node;
                Node.RangeStart = start;
                Node.RangeEnd = end;
            });
    }
}