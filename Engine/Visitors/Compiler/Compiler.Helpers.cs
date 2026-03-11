using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    private void EmitBoxingIfLiteral(SyntaxNode? node)
    {
        if (node is LiteralNode literal)
        {
            if (literal.Token.Kind == TokenKind.IntLiteral)
            {
                Emit(OpCodes.Box, typeof(int));
            }
            else if (literal.Token.Kind == TokenKind.FloatLiteral)
            {
                Emit(OpCodes.Box, typeof(double));
            }
            else if (literal.Token.Kind == TokenKind.TrueLiteral || literal.Token.Kind == TokenKind.FalseLiteral)
            {
                Emit(OpCodes.Box, typeof(bool));
            }
        }
    }

    private void EmitCoerceToBool()
    {
        var method = typeof(Runtime.Runtime).GetMethod("CoerceToBool", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;
        Emit(OpCodes.Call, method);
    }

    private PhpFunction? ResolveFunction(ExpressionNode? callee, in ReadOnlySpan<char> source)
    {
        if (callee is IdentifierNode identifierNode)
        {
            var name = identifierNode.Token.TextValue(in source);
            var fqn = string.IsNullOrEmpty(_currentNamespace) ? name : _currentNamespace + "\\" + name;
            var phpFunc = FunctionTable.GetFunction(fqn);
            if (phpFunc == null && !string.IsNullOrEmpty(_currentNamespace))
            {
                phpFunc = FunctionTable.GetFunction(name);
            }
            return phpFunc;
        }
        else if (callee is QualifiedNameNode qnameNode)
        {
            var fqnParts = new List<string>();
            foreach (var p in qnameNode.Parts)
                fqnParts.Add(p.TextValue(in source));

            string fqn = "";
            if (qnameNode.IsFullyQualified)
            {
                fqn = string.Join("\\", fqnParts);
            }
            else
            {
                if (_useImports.TryGetValue(fqnParts[0], out var imported))
                {
                    fqn = imported;
                    if (fqnParts.Count > 1) fqn += "\\" + string.Join("\\", fqnParts.Skip(1));
                }
                else
                {
                    fqn = string.IsNullOrEmpty(_currentNamespace) ? string.Join("\\", fqnParts) : _currentNamespace + "\\" + string.Join("\\", fqnParts);
                }
            }
            var phpFunc = FunctionTable.GetFunction(fqn);
            if (phpFunc == null && !qnameNode.IsFullyQualified && fqnParts.Count == 1 && !string.IsNullOrEmpty(_currentNamespace))
            {
                phpFunc = FunctionTable.GetFunction(fqnParts[0]); // Global fallback for single part
            }
            return phpFunc;
        }
        return null;
    }
}
