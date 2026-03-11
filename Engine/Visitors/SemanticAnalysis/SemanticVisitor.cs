using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.Loops;
using PHPIL.Engine.Visitors.SemanticAnalysis.Context;

namespace PHPIL.Engine.Visitors.SemanticAnalysis;

public partial class SemanticVisitor : IVisitor
{
    private readonly Stack<StackFrame> _currentContext = [];
    
    public void VisitForeachNode(ForeachNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitAnonymousFunctionNode(AnonymousFunctionNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }



    public void Visit(SyntaxNode node, in ReadOnlySpan<char> span)
    {
        throw new NotImplementedException();
    }

    public void VisitArgumentListNode(ArgumentListNode node, in ReadOnlySpan<char> source)
    {
        foreach (var arg in node.Arguments)
            arg.Accept(this, source);
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
        // throw new Exception($"Unknown {node.Token.TextValue(in source)}");
    }

    public void VisitUnaryOpNode(UnaryOpNode node, in ReadOnlySpan<char> source)
    {
        node.Operand?.Accept(this, source);
    }

    public void VisitElseNode(ElseNode node, in ReadOnlySpan<char> source)
    {
        node.Body?.Accept(this, source);
    }

    public void VisitSyntaxNode(SyntaxNode node, in ReadOnlySpan<char> source)
    {
    }

    public void VisitTernaryNode(TernaryNode node, in ReadOnlySpan<char> source)
    {
        node.Condition?.Accept(this, source);
        node.Then?.Accept(this, source);
        node.Else?.Accept(this, source);
    }

    public void VisitInterpolatedStringNode(InterpolatedStringNode node, in ReadOnlySpan<char> source)
    {
        foreach (var part in node.Parts)
            part.Accept(this, source);
    }

    public void VisitObjectAccessNode(ObjectAccessNode node, in ReadOnlySpan<char> source)
    {
        node.Object?.Accept(this, source);
        node.Property?.Accept(this, source);
    }
}