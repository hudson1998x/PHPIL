using System.Reflection;
using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.OOP;
using PHPIL.Engine.Visitors.SemanticAnalysis;
using PHPIL.Engine.Runtime;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    public void VisitFunctionCallNode(FunctionCallNode node, in ReadOnlySpan<char> source)
    {
        // Try instance method call first
        if (node.Callee is ObjectAccessNode objAccess)
        {
            // Instance method call: $obj->method(...)
            objAccess.Object?.Accept(this, source); // Push object
            var methodName = objAccess.Property.Token.TextValue(in source);
            MethodInfo? method = null;
            
            // Check if it's $this
            if (objAccess.Object is VariableNode varNode && varNode.Token.TextValue(in source) == "$this" && _currentType != null)
            {
                // Try to get method from TypeTable
                var fqn = _currentType.Name.Replace(".", "\\");
                method = TypeTable.GetMethod(fqn, methodName);
            }
            
            if (method != null)
            {
                foreach (var arg in node.Args) arg.Accept(this, source);
                Emit(OpCodes.Callvirt, method);
                return;
            }
            
            // Fall back to runtime helper for dynamic method calls
            Emit(OpCodes.Ldstr, methodName);
            var callMethod = typeof(PHPIL.Engine.Runtime.Sdk.RuntimeHelpers).GetMethod("CallMethod", new[] { typeof(object), typeof(string), typeof(object[]) })!;
            
            // Emit argument count
            Emit(OpCodes.Ldc_I4, node.Args.Count);
            Emit(OpCodes.Newarr, typeof(object));
            
            // Store args to array
            for (int i = 0; i < node.Args.Count; i++)
            {
                Emit(OpCodes.Dup);
                Emit(OpCodes.Ldc_I4, i);
                node.Args[i].Accept(this, source);
                EmitBoxingIfLiteral(node.Args[i]);
                Emit(OpCodes.Stelem_Ref);
            }
            
            Emit(OpCodes.Call, callMethod);
            return;
        }
        // Static method call
        else if (node.Callee is StaticAccessNode staticAccess)
        {
            string? methodName = null;
            if (staticAccess.MemberName is IdentifierNode idNode)
                methodName = idNode.Token.TextValue(in source);
            
            if (methodName == null)
                throw new Exception("Cannot resolve static method name");
                
            Type? targetType = null;
            if (staticAccess.Target is QualifiedNameNode qname)
            {
                var fqn = ResolveFQN(qname, source);
                targetType = TypeTable.GetType(fqn)?.RuntimeType;
            }
            if (targetType != null)
            {
                var method = targetType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                if (method != null)
                {
                    foreach (var arg in node.Args) arg.Accept(this, source);
                    Emit(OpCodes.Call, method);
                    return;
                }
            }
            throw new NotImplementedException($"Static method call '{methodName}' not found.");
        }
        // Variable call (e.g., $handle(...) or $myCallable(...)) - means we're calling a Closure
        else if (node.Callee is VariableNode)
        {
            var varName = ((VariableNode)node.Callee).Token.TextValue(in source);
            
            // First push the variable (which should be a Closure)
            node.Callee.Accept(this, source);

            // Then push arguments as object array
            Emit(OpCodes.Ldc_I4, node.Args.Count);
            Emit(OpCodes.Newarr, typeof(object));

            for (int i = 0; i < node.Args.Count; i++)
            {
                Emit(OpCodes.Dup);
                Emit(OpCodes.Ldc_I4, i);
                node.Args[i].Accept(this, source);
                EmitBoxingIfLiteral(node.Args[i]);
                Emit(OpCodes.Stelem_Ref);
            }

            // Call Closure.Invoke(object[] args)
            var invokeMethod = typeof(PHPIL.Engine.Runtime.Closure).GetMethod("Invoke", new[] { typeof(object[]) })!;
            Emit(OpCodes.Callvirt, invokeMethod);
            return;
        }
        // QualifiedName call (e.g., $handle(...) - could be a closure stored under a name)
        else if (node.Callee is QualifiedNameNode qname)
        {
            var parts = new List<string>();
            foreach (var p in qname.Parts)
                parts.Add(p.TextValue(in source));
            var qnameStr = string.Join("\\", parts);
            
            // Try to resolve as a function first
            var phpFunc2 = ResolveFunction(node.Callee, in source);
            if (phpFunc2 != null)
            {
                // It's a named function, use the normal path
                ResolveParamsAndCall(node, phpFunc2, source);
                return;
            }
            
            // If not found as a function, treat as a variable-based closure call
            // For now, throw an error
            throw new NotImplementedException($"Qualified name '{qnameStr}' is not a function or closure");
        }
        // Regular function call
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
