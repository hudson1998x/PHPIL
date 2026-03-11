using System;
using System.Collections.Generic;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure.OOP
{
    public class MethodNode : SyntaxNode
    {
        public PhpModifiers Modifiers { get; set; }
        public IdentifierNode Name { get; set; }
        public List<ParameterNode> Parameters { get; set; } = new();
        public IdentifierNode? ReturnType { get; set; }
        public BlockNode? Body { get; set; }

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitMethodNode(this, in source);
        }
    }
}
