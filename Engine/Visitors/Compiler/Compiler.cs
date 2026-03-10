using System.Reflection;
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
 
    private static readonly MethodInfo StringConcat =
        typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) })!;
    
    public void VisitBinaryOpNode(BinaryOpNode node, in ReadOnlySpan<char> source)
    {

        if (node.Operator is TokenKind.Concat)
        {
            node.Left?.Accept(this, source);
            EmitStringCoercion(node.Left!.AnalysedType);

            node.Right?.Accept(this, source);
            EmitStringCoercion(node.Right!.AnalysedType);

            Emit(OpCodes.Call, StringConcat);

            return;
        }

        node.Left?.Accept(this, source);
        node.Right?.Accept(this, source);
        
        switch (node.Operator)
        {
            case TokenKind.Multiply:
                Emit(OpCodes.Mul);
                break;
            case TokenKind.Add:
                Emit(OpCodes.Add);
                break;
            case TokenKind.Subtract:
                Emit(OpCodes.Sub);
                break;
            case TokenKind.DivideBy:
                Emit(OpCodes.Div);
                break;
            case TokenKind.Modulo:
                Emit(OpCodes.Rem);
                break;
            case TokenKind.Concat:
                Emit(OpCodes.Call, StringConcat);
                break;
            default:
                throw new NotImplementedException("Unknown operator: " + node.Operator);
        }
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
        // TODO: This causes stack balancing issues. 
        //       need to find a good way of dealing
        //       with this, might be a ref count
        // if (!node.IsUsed)
        // {
        //     // no point wasting any emissions here.
        //     return;
        // }

        if (node.VariableValue is VariableDeclaration childDeclaration)
        {
            // Visit the child first - this declares $b and stores 5 into it
            childDeclaration.Accept(this, source);
    
            // Now just load from the child's local and store into ours
            // No need for EmitValue on the child at all
            node.Local = DeclareLocal(TypeTable.GetPrimitive(node.AnalysedType));
            _locals[node.VariableName.TextValue(in source)] = node.Local;
    
            Emit(OpCodes.Ldloc, childDeclaration.Local!);
            Emit(OpCodes.Stloc, node.Local);
    
            if (node.EmitValue)
                Emit(OpCodes.Ldloc, node.Local);
        
            return; // important - skip the rest of the method
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
            _locals[node.VariableName.TextValue(in source)] = node.Local;
            
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
        if (node.Callee is not IdentifierNode identifierNode)
            throw new NotImplementedException("Dynamic function calling isn't supported yet");

        var phpFunc = FunctionTable.GetFunction(identifierNode.Token.TextValue(in source));
        if (phpFunc is null)
            throw new NotImplementedException($"The function {identifierNode.Token.TextValue(in source)} is not implemented yet");

        // --- Handle arguments ---
        for (int i = 0; i < node.Args.Count; i++)
        {
            node.Args[i].Accept(this, in source);
            EmitCoercion(node.Args[i].AnalysedType, phpFunc.ParameterTypes![i]);
        }

        // --- Ensure method exists ---
        if (phpFunc.Method?.Method is null)
            throw new InvalidOperationException("The PHP function doesn't have a method?");

        var returnType = phpFunc.Method.Method.ReturnType;

        // --- Call the PHP function ---
        Emit(OpCodes.Call, phpFunc.Method.Method);

        // --- Cast the return value if needed ---
        if (TypeTable.IsPrimitive(returnType))
        {
            // Example: cast int → string for concatenation
            // Replace AnalysedType.Int with the actual type mapping if available
            AnalysedType fromAnalysedType = returnType == typeof(int) ? AnalysedType.Int :
                returnType == typeof(bool) ? AnalysedType.Boolean :
                returnType == typeof(double) ? AnalysedType.Float :
                returnType == typeof(string) ? AnalysedType.String :
                throw new InvalidOperationException("Unknown primitive type");

            // Suppose you want it as string for concatenation
            Type targetType = typeof(string);
            TypeTable.CastPrimitive(GetIl(), fromAnalysedType, targetType);
        }
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
        if (_locals.TryGetValue(node.Token.TextValue(in source), out var local))
        {
            Emit(OpCodes.Ldloc, local);
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
        switch (node.Token.Kind)
        {
            case TokenKind.TrueLiteral:
                Emit(OpCodes.Ldc_I4_1);
                break;
            case TokenKind.FalseLiteral:
                Emit(OpCodes.Ldc_I4_0);
                break;
            case TokenKind.StringLiteral:
                Emit(OpCodes.Ldstr, node.Token.TextValue(in source).Trim(['"', '\'']));
                break;
            case TokenKind.NullLiteral:
                Emit(OpCodes.Ldnull);
                break;
            case TokenKind.IntLiteral:
                Emit(OpCodes.Ldc_I4, Int32.Parse(node.Token.TextValue(in source)));
                break;
            case TokenKind.FloatLiteral:
                Emit(OpCodes.Ldc_R8, double.Parse(node.Token.TextValue(in source)));
                break;
            default:
                throw new NotImplementedException();
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