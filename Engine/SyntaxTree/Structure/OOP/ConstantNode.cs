using System;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure.OOP
{
    public class ConstantNode : SyntaxNode
    {
        public PhpModifiers Modifiers { get; set; }
        public IdentifierNode Name { get; set; }
        public SyntaxNode Value { get; set; }

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitConstantNode(this, in source);
        }
    }
}
