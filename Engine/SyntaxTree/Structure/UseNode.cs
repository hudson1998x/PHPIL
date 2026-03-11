using System.Text;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure
{
    public class UseImport
    {
        public QualifiedNameNode Name { get; init; } = null!;
        public Token? Alias { get; init; }
    }

    public partial class UseNode : SyntaxNode
    {
        public List<UseImport> Imports { get; init; } = [];

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitUseNode(this, source);
        }
    }
}

namespace PHPIL.Engine.Visitors
{
    using PHPIL.Engine.SyntaxTree.Structure;
    public partial interface IVisitor
    {
        void VisitUseNode(UseNode node, in ReadOnlySpan<char> source);
    }
}
