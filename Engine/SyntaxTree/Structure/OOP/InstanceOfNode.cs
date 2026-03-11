using System;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure.OOP
{
    public class InstanceOfNode : ExpressionNode
    {
        public ExpressionNode Expression { get; set; }
        public ExpressionNode ClassIdentifier { get; set; }

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitInstanceOfNode(this, in source);
        }
    }
}
