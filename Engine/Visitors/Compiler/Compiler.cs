using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.Loops;

namespace PHPIL.Engine.Visitors;

public partial class Compiler : IVisitor
{
    public void Visit(SyntaxNode node, in ReadOnlySpan<char> span)
    {
        throw new NotImplementedException();
    }



    public void VisitArgumentListNode(ArgumentListNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitElseIfNode(ElseIfNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitGroupNode(GroupNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitIdentifierNode(IdentifierNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitUnaryOpNode(UnaryOpNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitElseNode(ElseNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitSyntaxNode(SyntaxNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitTernaryNode(TernaryNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }
    
    public void VisitForeachNode(ForeachNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitAnonymousFunctionNode(AnonymousFunctionNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }


}