using System.Text;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure
{
    /// <summary>
    /// Represents an object property access: $obj->prop
    /// TODO: This is currently a stub until classes/objects are implemented.
    /// </summary>
    public partial class ObjectAccessNode : ExpressionNode
    {
        public ExpressionNode Object { get; set; } = null!;
        public IdentifierNode Property { get; set; } = null!;

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitObjectAccessNode(this, source);
        }

        public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
        {
            builder.Append("{\"type\":\"ObjectAccessNode\",\"object\":");
            Object.ToJson(in span, in tokens, builder);
            builder.Append(",\"property\":");
            Property.ToJson(in span, in tokens, builder);
            builder.Append("}");
        }
    }
}

namespace PHPIL.Engine.Visitors
{
    using PHPIL.Engine.SyntaxTree.Structure;
    public partial interface IVisitor
    {
        void VisitObjectAccessNode(ObjectAccessNode node, in ReadOnlySpan<char> source);
    }
}

