using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure.OOP
{
    public class ParameterNode : SyntaxNode
    {
        public IdentifierNode? TypeHint { get; set; }
        public IdentifierNode Name { get; set; } = null!;
        public SyntaxNode? DefaultValue { get; set; }

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> span)
        {
            // ParameterNode is visited as part of method/function compilation
        }
    }
}
