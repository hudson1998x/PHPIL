using System;
using System.Collections.Generic;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure.OOP
{
    public class NewNode : ExpressionNode
    {
        public ExpressionNode ClassIdentifier { get; set; }
        public List<SyntaxNode> Arguments { get; set; } = new();

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitNewNode(this, in source);
        }
    }
}
