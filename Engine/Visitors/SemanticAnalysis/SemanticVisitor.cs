using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.Loops;

namespace PHPIL.Engine.Visitors.SemanticAnalysis;

public class SemanticVisitor : IVisitor
{
    public void VisitArrayLiteralNode(ArrayLiteralNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitPrefixExpressionNode(PrefixExpressionNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitPostfixExpressionNode(PostfixExpressionNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitForeachNode(ForeachNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitWhileNode(WhileNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitAnonymousFunctionNode(AnonymousFunctionNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitFunctionNode(FunctionNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitFunctionParameter(FunctionParameter node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitForNode(For node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitVariableDeclaration(VariableDeclaration node, in ReadOnlySpan<char> source)
    {
        node.VariableValue?.Accept(this, source);
        
        node.AnalysedType = node.VariableValue?.AnalysedType ?? AnalysedType.Mixed;
    }

    public void VisitBinaryOpNode(BinaryOpNode node, in ReadOnlySpan<char> source)
    {
        node.Left?.Accept(this, source);
        node.Right?.Accept(this, source);

        if (node.Left is { } leftNode && node.Right is { } rightNode)
        {
            node.AnalysedType = InferBinaryOpType(node.Operator,  leftNode.AnalysedType, rightNode.AnalysedType);
            return;
        }

        if (node.Left is { })
        {
            node.AnalysedType = node.Left.AnalysedType;
            return;
        }

        if (node.Right is { })
        {
            node.AnalysedType = node.Right.AnalysedType;
        }
    }

    public void Visit(SyntaxNode node, in ReadOnlySpan<char> span)
    {
        throw new NotImplementedException();
    }

    public void VisitBlockNode(BlockNode node, in ReadOnlySpan<char> source)
    {
        foreach (var stmt in node.Statements)
        {
            stmt.Accept(this, in source);
        }
    }

    public void VisitFunctionCallNode(FunctionCallNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitExpressionNode(ExpressionNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitReturnNode(ReturnNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitVariableNode(VariableNode node, in ReadOnlySpan<char> source)
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

    public void VisitLiteralNode(LiteralNode node, in ReadOnlySpan<char> source)
    {
        Console.WriteLine("[LITERALNODE-CALLED]");
    }

    public void VisitUnaryOpNode(UnaryOpNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitBreakNode(BreakNode node, in ReadOnlySpan<char> source)
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

    public void VisitIfNode(IfNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitTernaryNode(TernaryNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }


    private static AnalysedType InferBinaryOpType(TokenKind op, AnalysedType left, AnalysedType right)
    {
        if (left == AnalysedType.Mixed || right == AnalysedType.Mixed)
            return AnalysedType.Mixed;

        if (left == AnalysedType.Float || right == AnalysedType.Float)
            return AnalysedType.Float;

        if (left == AnalysedType.Int && right == AnalysedType.Int)
            return AnalysedType.Int;
        
        return AnalysedType.Mixed;
    }
}