using System;
using System.Collections.Generic;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure.OOP
{
    public class ClassNode : SyntaxNode
    {
        public QualifiedNameNode Name { get; set; }
        public QualifiedNameNode? Extends { get; set; }
        public List<QualifiedNameNode> Implements { get; set; } = new();
        public List<SyntaxNode> Members { get; set; } = new();
        public bool IsAbstract { get; set; }
        public bool IsFinal { get; set; }

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitClassNode(this, in source);
        }
    }
}
