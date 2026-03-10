using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.Loops;
using PHPIL.Engine.Visitors.SemanticAnalysis.Context;
using PHPIL.Engine.Visitors.SemanticAnalysis.Context.PHPIL.Engine.Visitors.SemanticAnalysis.Context;

namespace PHPIL.Engine.Visitors.SemanticAnalysis;

public class SemanticVisitor : IVisitor
{
    private Stack<StackFrame> _currentContext = new();

    public void VisitArrayLiteralNode(ArrayLiteralNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }
    
    public void VisitContinueNode(ContinueNode node, in ReadOnlySpan<char> source)
    {
        // nothing to analyse
    }

    public void VisitPrefixExpressionNode(PrefixExpressionNode node, in ReadOnlySpan<char> source)
    {
        node.Operand.Accept(this, source);
    }

    public void VisitPostfixExpressionNode(PostfixExpressionNode node, in ReadOnlySpan<char> source)
    {
        node.Operand.Accept(this, source);
    }

    public void VisitForeachNode(ForeachNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitWhileNode(WhileNode node, in ReadOnlySpan<char> source)
    {
        _currentContext.Push(new StackFrame { CanAscend = true });

        if (node.Expression != null)
            node.Expression.Accept(this, source);

        if (node.Body != null)
            node.Body.Accept(this, source);

        _currentContext.Pop();
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
        _currentContext.Push(new StackFrame { CanAscend = true });

        if (node.Init != null)
            node.Init.Accept(this, source);

        if (node.Init != null)
            node.Init.Accept(this, source);

        if (node.Condition != null)
            node.Condition.Accept(this, source);

        if (node.Increment != null)
            node.Increment.Accept(this, source);

        if (node.Body != null)
            node.Body.Accept(this, source);

        _currentContext.Pop();
    }

    public void VisitVariableDeclaration(VariableDeclaration node, in ReadOnlySpan<char> source)
    {
        if (node.VariableValue is not null)
        {
            node.VariableValue.Accept(this, source);

            if (node.VariableValue is VariableDeclaration nested)
                nested.EmitValue = true;
        }

        node.AnalysedType = node.VariableValue?.AnalysedType ?? AnalysedType.Mixed;

        ResolveOrDeclareVariable(node.VariableName.TextValue(in source), node.AnalysedType, node);
    }

    public void VisitBinaryOpNode(BinaryOpNode node, in ReadOnlySpan<char> source)
    {
        node.Left?.Accept(this, source);
        node.Right?.Accept(this, source);

        node.AnalysedType = node.Operator switch
        {
            TokenKind.Concat   => AnalysedType.String,
            TokenKind.Multiply => node.Left!.AnalysedType is AnalysedType.Float || node.Right!.AnalysedType is AnalysedType.Float
                                    ? AnalysedType.Float
                                    : AnalysedType.Int,
            _ => AnalysedType.Mixed
        };
    }

    public void Visit(SyntaxNode node, in ReadOnlySpan<char> span)
    {
        throw new NotImplementedException();
    }

    public void VisitBlockNode(BlockNode node, in ReadOnlySpan<char> source)
    {
        foreach (var stmt in node.Statements)
            stmt.Accept(this, in source);
    }

    public void VisitFunctionCallNode(FunctionCallNode node, in ReadOnlySpan<char> source)
    {
        foreach (var arg in node.Args)
            arg.Accept(this, source);
    }

    public void VisitExpressionNode(ExpressionNode node, in ReadOnlySpan<char> source)
    {
        foreach (var stmt in node.Statements)
        {
            stmt.Accept(this, in source);
        }
    }

    public void VisitReturnNode(ReturnNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitVariableNode(VariableNode node, in ReadOnlySpan<char> source)
    {
        var name = node.Token.TextValue(in source);

        VariableInfo? info = null;

        foreach (var frame in _currentContext.Reverse())
        {
            if (frame.Variables.TryGetValue(name, out info))
            {
                if (_currentContext.Count > 1)
                    info.IsCaptured = true;

                info.IsUsed = true;
                node.AnalysedType = info.Type;

                if (info.Node != null)
                    node.AnalysedType = info.Node.AnalysedType;

                return;
            }

            if (!frame.CanAscend) break;
        }

        if (_currentContext.Count > 0)
        {
            var currentFrame = _currentContext.Peek();
            var newInfo = new VariableInfo(node.AnalysedType, null)
            {
                IsUsed = true,
                IsCaptured = _currentContext.Count > 1
            };
            currentFrame.Variables[name] = newInfo;
        }
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
        // throw new Exception($"Unknown {node.Token.TextValue(in source)}");
    }

    public void VisitLiteralNode(LiteralNode node, in ReadOnlySpan<char> source)
    {
        node.AnalysedType = node.Token.Kind switch
        {
            TokenKind.IntLiteral    => AnalysedType.Int,
            TokenKind.FloatLiteral  => AnalysedType.Float,
            TokenKind.StringLiteral => AnalysedType.String,
            TokenKind.TrueLiteral   => AnalysedType.Boolean,
            TokenKind.FalseLiteral  => AnalysedType.Boolean,
            TokenKind.NullLiteral   => AnalysedType.Mixed,
            _                       => AnalysedType.Mixed
        };
    }

    public void VisitUnaryOpNode(UnaryOpNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitBreakNode(BreakNode node, in ReadOnlySpan<char> source)
    {
        
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
        if (node.Expression != null)
            node.Expression.Accept(this, source);

        if (node.Body != null)
        {
            _currentContext.Push(new StackFrame { CanAscend = true });
            node.Body.Accept(this, source);
            _currentContext.Pop();
        }

        foreach (var elseIf in node.ElseIfs)
            elseIf.Accept(this, source);

        if (node.ElseNode != null)
            node.ElseNode.Accept(this, source);
    }

    public void VisitTernaryNode(TernaryNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    private void ResolveOrDeclareVariable(
        string name,
        AnalysedType type,
        VariableDeclaration? node = null,
        bool fromNestedScope = false,
        bool markUsed = false)
    {
        foreach (var frame in _currentContext.Reverse())
        {
            if (frame.Variables.TryGetValue(name, out var info))
            {
                info.Type = type;
                if (node != null) info.Node ??= node;
                if (fromNestedScope) info.IsCaptured = true;
                if (markUsed) info.IsUsed = true;
                return;
            }

            if (!frame.CanAscend) break;
        }

        if (_currentContext.Count > 0)
        {
            var currentFrame = _currentContext.Peek();
            currentFrame.Variables[name] = new VariableInfo(type, node!);
            if (fromNestedScope) currentFrame.Variables[name].IsCaptured = true;
            if (markUsed) currentFrame.Variables[name].IsUsed = true;
        }
        else
        {
            var globalFrame = new StackFrame();
            globalFrame.Variables[name] = new VariableInfo(type, node!);
            if (fromNestedScope) globalFrame.Variables[name].IsCaptured = true;
            if (markUsed) globalFrame.Variables[name].IsUsed = true;
            _currentContext.Push(globalFrame);
        }
    }
}