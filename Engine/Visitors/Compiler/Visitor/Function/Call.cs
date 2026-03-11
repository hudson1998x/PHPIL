using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
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

        var methodToCall = phpFunc.MethodInfo ?? phpFunc.Method?.Method;
        if (methodToCall is null)
            throw new InvalidOperationException("The PHP function doesn't have a method?");

        var returnType = methodToCall.ReturnType;

        Emit(OpCodes.Call, methodToCall);

        if (returnType != typeof(void))
        {
            AnalysedType fromAnalysedType = returnType == typeof(int) ? AnalysedType.Int :
                returnType == typeof(bool) ? AnalysedType.Boolean :
                returnType == typeof(double) ? AnalysedType.Float :
                returnType == typeof(string) ? AnalysedType.String :
                AnalysedType.Mixed;

            EmitCoercion(fromAnalysedType, typeof(object));
        }
    }
}