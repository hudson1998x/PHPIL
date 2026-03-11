using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    public void VisitFunctionCallNode(FunctionCallNode node, in ReadOnlySpan<char> source)
    {
        var phpFunc = ResolveFunction(node.Callee, in source);
        if (phpFunc == null)
        {
            string name = "unknown";
            if (node.Callee is IdentifierNode id)
            {
                name = id.Token.TextValue(in source);
            }
            else if (node.Callee is QualifiedNameNode qn)
            {
                var parts = new List<string>();
                foreach (var p in qn.Parts)
                    parts.Add(p.TextValue(in source));
                name = string.Join("\\", parts);
            }
            throw new NotImplementedException($"The function {name} is not implemented yet");
        }

        ResolveParamsAndCall(node, phpFunc, source);
    }

    private void ResolveParamsAndCall(FunctionCallNode node, PhpFunction phpFunc, ReadOnlySpan<char> source)
    {

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