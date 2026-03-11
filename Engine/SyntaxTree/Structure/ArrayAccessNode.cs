using System.Text;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure;

public class ArrayAccessNode : ExpressionNode
{
    public ExpressionNode Array { get; set; } = null!;
    public ExpressionNode? Key { get; set; } // Nullable for $arr[] = ...

    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        => visitor.VisitArrayAccessNode(this, source);

    public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append("{\"type\":\"ArrayAccessNode\",\"array\":");
        Array.ToJson(in span, in tokens, builder);
        builder.Append(",\"key\":");
        if (Key != null)
            Key.ToJson(in span, in tokens, builder);
        else
            builder.Append("null");
        builder.Append("}");
    }
}
