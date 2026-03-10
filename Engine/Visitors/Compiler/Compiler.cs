using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.Loops;
using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.Visitors;

public partial class Compiler : IVisitor
{
    public void VisitBinaryOpNode(BinaryOpNode node, in ReadOnlySpan<char> source)
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

    public void VisitArrayLiteralNode(ArrayLiteralNode node, in ReadOnlySpan<char> source)
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
        if (!node.IsUsed)
        {
            // no point wasting any emissions here.
            return;
        }

        if (node.VariableValue is not null)
        {
            node.VariableValue.Accept(this, source);
        }
        else
        {
            Emit(OpCodes.Ldnull);
        }

        if (node.AnalysedType is not AnalysedType.Object)
        {
            // primitive :)
            node.Local = DeclareLocal(TypeTable.GetPrimitive(node.AnalysedType));
            Emit(OpCodes.Stloc, node.Local);

            if (node.EmitValue)
            {
                Emit(OpCodes.Ldloc, node.Local);
            }
            return;
        }

        throw new NotImplementedException("Non primitive type invocation, this is not implemented yet, primitive values only.");
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
        switch (node.Token.Kind)
        {
            case TokenKind.TrueLiteral:
                Emit(OpCodes.Ldc_I4_1);
                break;
            case TokenKind.FalseLiteral:
                Emit(OpCodes.Ldc_I4_0);
                break;
            case TokenKind.StringLiteral:
                Emit(OpCodes.Ldstr, node.Token.TextValue(in source));
                break;
            case TokenKind.NullLiteral:
                Emit(OpCodes.Ldnull);
                break;
            case TokenKind.IntLiteral:
                Emit(OpCodes.Ldc_I4_1, Int16.Parse(node.Token.TextValue(in source)));
                break;
            case TokenKind.FloatLiteral:
                Emit(OpCodes.Ldc_R8, double.Parse(node.Token.TextValue(in source)));
                break;
        }
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
}