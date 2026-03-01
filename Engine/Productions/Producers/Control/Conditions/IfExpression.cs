using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Producers;

public class IfExpression : KeywordExpressionBlock
{
    public new IfNode? Node { get; private set; }
    public List<ElseIfNode> ElseIfs { get; } = new();
    public BlockNode? Else { get; private set; }

    public override SyntaxNode CreateNode(int start, int end, ExpressionNode? expr, BlockNode? body)
    {
        return new IfNode
        {
            RangeStart = start,
            RangeEnd = end,
            Expression = expr,
            Body = body
        };
    }

    public override Producer Init()
    {
        var baseProducer = base.Init();

        // Single reusable else expression instance
        var elseInstance = new ElseExpression();

        return (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            // Run the base if(expr) { block } match first
            var baseMatch = baseProducer(tokens, source, pointer);
            if (!baseMatch.Success)
                return baseMatch;

            int current = baseMatch.End;

            var whitespaceOrNewline = AnyOf(Prefab<Whitespace>(), Prefab<NewLine>());

            // Skip whitespace between blocks
            var wsMatch = Optional(whitespaceOrNewline)(tokens, source, current);
            current = wsMatch.End;

            // Collect else-if nodes
            var elseIfInstances = new List<ElseIfExpression>();
            while (true)
            {
                var instance = new ElseIfExpression();
                var match = instance.Init()(tokens, source, current);
                if (!match.Success)
                    break;

                current = match.End;
                elseIfInstances.Add(instance);
            }

            // Skip whitespace before else
            wsMatch = Optional(whitespaceOrNewline)(tokens, source, current);
            current = wsMatch.End;

            // Optional else
            var elseMatch = Optional(elseInstance.Init())(tokens, source, current);
            current = elseMatch.End;

            // Efficiently filter null ElseIfs
            ElseIfs.Clear();
            foreach (var e in elseIfInstances)
            {
                if (e?.Node?.ElseNode is not null)
                    ElseIfs.Add(e.Node);
            }

            Else = elseInstance.Node;

            Node = new IfNode
            {
                RangeStart = baseMatch.Start,
                RangeEnd   = current,
                Expression = (base.Node as IfNode)?.Expression,
                Body       = (base.Node as IfNode)?.Body,
                ElseIfs    = ElseIfs,
                ElseNode   = Else
            };

            return new Match(true, baseMatch.Start, current);
        };
    }
}