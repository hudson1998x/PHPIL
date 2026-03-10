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

    /// <summary>
    /// Handles variable declarations: sets the type, stores node reference, and resolves in the current frame
    /// </summary>
    public void VisitVariableDeclaration(VariableDeclaration node, in ReadOnlySpan<char> source)
    {
        // First visit the RHS expression
        node.VariableValue?.Accept(this, source);

        // Determine the analysed type
        node.AnalysedType = node.VariableValue?.AnalysedType ?? AnalysedType.Mixed;

        // Resolve or declare in current/nested frame
        ResolveOrDeclareVariable(node.VariableName.TextValue(in source), node.AnalysedType, node);
    }

    public void VisitBinaryOpNode(BinaryOpNode node, in ReadOnlySpan<char> source)
    {
        node.Left?.Accept(this, source);
        node.Right?.Accept(this, source);
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

    /// <summary>
    /// Handles reads of variables
    /// </summary>
    public void VisitVariableNode(VariableNode node, in ReadOnlySpan<char> source)
    {
        var name = node.Token.TextValue(in source);

        VariableInfo? info = null;

        // Try to resolve variable in the context stack
        foreach (var frame in _currentContext.Reverse())
        {
            if (frame.Variables.TryGetValue(name, out info))
            {
                // Mark captured if in nested scope
                if (_currentContext.Count > 1)
                    info.IsCaptured = true;

                // Mark used
                info.IsUsed = true;

                // Update the VariableNode's evaluated type from resolved variable
                node.AnalysedType = info.Type;

                // Also propagate to AST node reference if needed
                if (info.Node != null)
                {
                    node.AnalysedType = info.Node.AnalysedType;
                }

                return;
            }

            if (!frame.CanAscend) break;
        }

        // Not found → declare in current frame with Mixed type
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
        throw new NotImplementedException();
    }

    public void VisitLiteralNode(LiteralNode node, in ReadOnlySpan<char> source)
    {
        
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
    
    /// <summary>
    /// Resolve a variable in the current context stack, declare if missing.
    /// Updates type, AST node reference, and sets captured/used flags.
    /// </summary>
    private void ResolveOrDeclareVariable(
        string name,
        AnalysedType type,
        VariableDeclaration? node = null,
        bool fromNestedScope = false,
        bool markUsed = false
    )
    {
        foreach (var frame in _currentContext.Reverse())
        {
            if (frame.Variables.TryGetValue(name, out var info))
            {
                // Update type and node reference
                info.Type = type;
                if (node != null) info.Node ??= node;

                // Flags
                if (fromNestedScope) info.IsCaptured = true;
                if (markUsed) info.IsUsed = true;
                return;
            }

            if (!frame.CanAscend) break;
        }

        // Not found → declare in current frame
        if (_currentContext.Count > 0)
        {
            var currentFrame = _currentContext.Peek();
            currentFrame.Variables[name] = new VariableInfo(type, node!);

            if (fromNestedScope) currentFrame.Variables[name].IsCaptured = true;
            if (markUsed) currentFrame.Variables[name].IsUsed = true;
        }
        else
        {
            // fallback global frame
            var globalFrame = new StackFrame();
            globalFrame.Variables[name] = new VariableInfo(type, node!);
            if (fromNestedScope) globalFrame.Variables[name].IsCaptured = true;
            if (markUsed) globalFrame.Variables[name].IsUsed = true;
            _currentContext.Push(globalFrame);
        }
    }
}