using System.Text;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.SyntaxTree.Structure;

public class ArrayItemNode : SyntaxNode
{
    public ExpressionNode? Key { get; set; }
    public ExpressionNode Value { get; set; } = null!;

    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
    {
        Key?.Accept(visitor, source);
        Value.Accept(visitor, source);
    }

    public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append("{");
        if (Key != null)
        {
            builder.Append("\"key\":");
            Key.ToJson(in span, in tokens, builder);
            builder.Append(",");
        }
        builder.Append("\"value\":");
        Value.ToJson(in span, in tokens, builder);
        builder.Append("}");
    }
}

public class ArrayLiteralNode : ExpressionNode
{
    public List<ArrayItemNode> Items { get; set; } = new List<ArrayItemNode>();
    
    public bool IsAssociative { get; set; }

    public override AnalysedType AnalysedType { get; set; }

    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        => visitor.VisitArrayLiteralNode(this, source);

    public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append("{\"type\":\"ArrayLiteralNode\",\"items\":[");
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].ToJson(in span, in tokens, builder);
            if (i < Items.Count - 1) builder.Append(",");
        }
        builder.Append("]}");
    }
}

public class SpreadNode : ExpressionNode
{
    public ExpressionNode Expression { get; set; } = null!;

    public override AnalysedType AnalysedType { get; set; } = AnalysedType.Mixed;

    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        => visitor.VisitSpreadNode(this, source);

    public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append("{\"type\":\"SpreadNode\",\"expression\":");
        Expression.ToJson(in span, in tokens, builder);
        builder.Append("}");
    }
}