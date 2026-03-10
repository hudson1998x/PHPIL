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
            EmitStringCoercion(node.Left!.AnalysedType, isVariable: node.Left is VariableNode);

            node.Right?.Accept(this, source);
            EmitStringCoercion(node.Right!.AnalysedType, isVariable: node.Right is VariableNode);

            Emit(OpCodes.Call, StringConcat);
            return;
        }

        node.Left?.Accept(this, source);
        if (node.Left is VariableNode)
            Emit(OpCodes.Unbox_Any, typeof(int));

        node.Right?.Accept(this, source);
        if (node.Right is VariableNode)
            Emit(OpCodes.Unbox_Any, typeof(int));

        switch (node.Operator)
        {
            case TokenKind.Multiply: Emit(OpCodes.Mul);  break;
            case TokenKind.Add:      Emit(OpCodes.Add);  break;
            case TokenKind.Subtract: Emit(OpCodes.Sub);  break;
            case TokenKind.DivideBy: Emit(OpCodes.Div);  break;
            case TokenKind.Modulo:   Emit(OpCodes.Rem);  break;
            case TokenKind.LessThan: Emit(OpCodes.Clt);  break;
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
        if (node.Operator.Kind == TokenKind.Increment)
        {
            if (node.Operand is not VariableNode varNode)
                throw new Exception("Increment requires a variable.");

            string varName = varNode.Token.TextValue(in source);
            if (!_locals.TryGetValue(varName, out var local))
                throw new Exception($"Undefined variable: {varName}");

            Emit(OpCodes.Ldloc, local);
            Emit(OpCodes.Ldloc, local);
            Emit(OpCodes.Unbox_Any, typeof(int));
            Emit(OpCodes.Ldc_I4_1);
            Emit(OpCodes.Add);
            Emit(OpCodes.Box, typeof(int));
            Emit(OpCodes.Stloc, local);

            // Result on stack: [Old Value]
        }
    }

    public void VisitForNode(For node, in ReadOnlySpan<char> source)
    {
        var conditionLabel = DefineLabel();
        var exitLabel = DefineLabel();

        if (node.Init != null)
            node.Init.Accept(this, source);

        MarkLabel(conditionLabel);

        if (node.Condition != null)
        {
            node.Condition.Accept(this, source);
            Emit(OpCodes.Brfalse, exitLabel);
        }

        if (node.Body != null)
            node.Body.Accept(this, source);

        if (node.Increment != null)
        {
            node.Increment.Accept(this, source);
            Emit(OpCodes.Pop);
        }

        Emit(OpCodes.Br, conditionLabel);

        MarkLabel(exitLabel);
    }

    public void VisitVariableDeclaration(VariableDeclaration node, in ReadOnlySpan<char> source)
    {
        if (node.VariableValue is VariableDeclaration childDeclaration)
        {
            childDeclaration.Accept(this, source);

            node.Local = DeclareLocal(typeof(object));
            _locals[node.VariableName.TextValue(in source)] = node.Local;

            Emit(OpCodes.Ldloc, childDeclaration.Local!);
            Emit(OpCodes.Stloc, node.Local);

            if (node.EmitValue)
                Emit(OpCodes.Ldloc, node.Local);

            return;
        }

        if (node.VariableValue is not null)
            node.VariableValue.Accept(this, source);
        else
            Emit(OpCodes.Ldnull);

        node.Local = DeclareLocal(typeof(object));
        _locals[node.VariableName.TextValue(in source)] = node.Local;

        switch (node.AnalysedType)
        {
            case AnalysedType.Int:     Emit(OpCodes.Box, typeof(int));    break;
            case AnalysedType.Float:   Emit(OpCodes.Box, typeof(double)); break;
            case AnalysedType.Boolean: Emit(OpCodes.Box, typeof(bool));   break;
            case AnalysedType.Mixed:   Emit(OpCodes.Box, typeof(int));    break;
        }

        Emit(OpCodes.Stloc, node.Local);

        if (node.EmitValue)
            Emit(OpCodes.Ldloc, node.Local);
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
        if (node.Callee is not IdentifierNode identifierNode)
            throw new NotImplementedException("Dynamic function calling isn't supported yet");

        var phpFunc = FunctionTable.GetFunction(identifierNode.Token.TextValue(in source));
        if (phpFunc is null)
            throw new NotImplementedException($"The function {identifierNode.Token.TextValue(in source)} is not implemented yet");

        for (int i = 0; i < node.Args.Count; i++)
        {
            node.Args[i].Accept(this, in source);
            EmitCoercion(node.Args[i].AnalysedType, phpFunc.ParameterTypes![i]);
        }

        if (phpFunc.Method?.Method is null)
            throw new InvalidOperationException("The PHP function doesn't have a method?");

        var returnType = phpFunc.Method.Method.ReturnType;

        Emit(OpCodes.Call, phpFunc.Method.Method);

        if (TypeTable.IsPrimitive(returnType))
        {
            AnalysedType fromAnalysedType = returnType == typeof(int) ? AnalysedType.Int :
                returnType == typeof(bool) ? AnalysedType.Boolean :
                returnType == typeof(double) ? AnalysedType.Float :
                returnType == typeof(string) ? AnalysedType.String :
                throw new InvalidOperationException("Unknown primitive type");

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
        if (!_locals.TryGetValue(node.Token.TextValue(in source), out var local))
            throw new Exception($"Undefined variable: {node.Token.TextValue(in source)}");

        Emit(OpCodes.Ldloc, local);
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
            case TokenKind.TrueLiteral:   Emit(OpCodes.Ldc_I4_1); break;
            case TokenKind.FalseLiteral:  Emit(OpCodes.Ldc_I4_0); break;
            case TokenKind.StringLiteral: Emit(OpCodes.Ldstr, node.Token.TextValue(in source).Trim(['"', '\''])); break;
            case TokenKind.NullLiteral:   Emit(OpCodes.Ldnull); break;
            case TokenKind.IntLiteral:    Emit(OpCodes.Ldc_I4, Int32.Parse(node.Token.TextValue(in source))); break;
            case TokenKind.FloatLiteral:  Emit(OpCodes.Ldc_R8, double.Parse(node.Token.TextValue(in source))); break;
            default: throw new NotImplementedException();
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
}