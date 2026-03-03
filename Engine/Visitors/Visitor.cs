using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

/// <summary>
/// A composite visitor that broadcasts each <see cref="SyntaxNode"/> to an
/// ordered collection of inner <see cref="IVisitor"/> implementations.
/// </summary>
public class Visitor : IVisitor
{
    private readonly IVisitor[] _visitors;

    /// <summary>
    /// Initialises the composite with the set of visitors to broadcast to.
    /// </summary>
    public Visitor(params IVisitor[] visitors)
    {
        _visitors = visitors;
    }

    /// <summary>
    /// The primary entry point for the composite visitor.
    /// Dispatches the node to every inner visitor via Accept (Double Dispatch).
    /// </summary>
    public void Visit(SyntaxNode node, in ReadOnlySpan<char> span)
    {
        foreach (var visitor in _visitors)
        {
            // We use Accept to ensure the most specific Visit method is called on the inner visitors
            node.Accept(visitor, in span);
        }
    }

    public void VisitArgumentListNode(ArgumentListNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitArgumentListNode(node, in source);
    }

    public void VisitPostfixExpressionNode(PostfixExpressionNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitPostfixExpressionNode(node, in source);
    }

    public void VisitPrefixExpressionNode(PrefixExpressionNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitPrefixExpressionNode(node, in source);
    }

    public void VisitBreakNode(BreakNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitBreakNode(node, in source);
    }

    public void VisitVariableDeclaration(VariableDeclaration node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitVariableDeclaration(node, in source);
    }

    public void VisitVariableNode(VariableNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitVariableNode(node, in source);
    }

    public void VisitUnaryOpNode(UnaryOpNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitUnaryOpNode(node, in source);
    }

    public void VisitTernaryNode(TernaryNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitTernaryNode(node, in source);
    }

    public void VisitReturnNode(ReturnNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitReturnNode(node, in source);
    }

    public void VisitLiteralNode(LiteralNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitLiteralNode(node, in source);
    }

    public void VisitIdentifierNode(IdentifierNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitIdentifierNode(node, in source);
    }

    public void VisitGroupNode(GroupNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitGroupNode(node, in source);
    }

    public void VisitExpressionNode(ExpressionNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitExpressionNode(node, in source);
    }

    public void VisitBlockNode(BlockNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitBlockNode(node, in source);
    }

    public void VisitBinaryOpNode(BinaryOpNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitBinaryOpNode(node, in source);
    }

    public void VisitWhileNode(WhileNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitWhileNode(node, in source);
    }

    public void VisitForNode(For node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitForNode(node, in source);
    }

    public void VisitIfNode(IfNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitIfNode(node, in source);
    }

    public void VisitElseNode(ElseNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitElseNode(node, in source);
    }

    public void VisitFunctionCallNode(FunctionCallNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitFunctionCallNode(node, in source);
    }

    public void VisitAnonymousFunctionNode(AnonymousFunctionNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitAnonymousFunctionNode(node, in source);
    }

    public void VisitElseIfNode(ElseIfNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitElseIfNode(node, in source);
    }

    public void VisitFunctionParameter(FunctionParameter node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitFunctionParameter(node, in source);
    }

    public void VisitSyntaxNode(SyntaxNode node, in ReadOnlySpan<char> source)
    {
        foreach (var visitor in _visitors) visitor.VisitSyntaxNode(node, in source);
    }
}