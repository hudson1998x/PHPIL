using System;
using System.Collections.Generic;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure.OOP
{
    public class InterfaceNode : SyntaxNode
    {
        public QualifiedNameNode Name { get; set; }
        public List<QualifiedNameNode> Extends { get; set; } = new();
        public List<SyntaxNode> Members { get; set; } = new();

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitInterfaceNode(this, in source);
        }
    }
}
