using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.OOP;

namespace PHPIL.Engine.Visitors;


public partial class Compiler
{
    public void VisitLiteralNode(LiteralNode node, in ReadOnlySpan<char> source)
    {
        switch (node.Token.Kind)
        {
            case TokenKind.TrueLiteral:   Emit(OpCodes.Ldc_I4_1); break;
            case TokenKind.FalseLiteral:  Emit(OpCodes.Ldc_I4_0); break;
            case TokenKind.StringLiteral: 
                var val = node.Token.TextValue(in source);
                if (val.StartsWith("'") || val.StartsWith("\"")) val = val.Substring(1, val.Length - 2);
                Emit(OpCodes.Ldstr, val); 
                break;
            case TokenKind.NullLiteral:   Emit(OpCodes.Ldnull); break;
            case TokenKind.IntLiteral:    Emit(OpCodes.Ldc_I4, Int32.Parse(node.Token.TextValue(in source))); break;
            case TokenKind.FloatLiteral:  Emit(OpCodes.Ldc_R8, double.Parse(node.Token.TextValue(in source))); break;
            default: throw new NotImplementedException();
        }
    }

    public void VisitInterpolatedStringNode(InterpolatedStringNode node, in ReadOnlySpan<char> source)
    {
        if (node.Parts.Count == 0)
        {
            Emit(OpCodes.Ldstr, "");
            return;
        }

        // Build an array of strings and concatenate
        Emit(OpCodes.Ldc_I4, node.Parts.Count);
        Emit(OpCodes.Newarr, typeof(string));
        
        int stackOffset = 0;
        for (int i = 0; i < node.Parts.Count; i++)
        {
            // Duplicate array
            Emit(OpCodes.Dup);
            // Push index
            Emit(OpCodes.Ldc_I4, i);
            
            // Emit the part
            var part = node.Parts[i];
            part.Accept(this, source);
            
            // Handle both VariableNode and ObjectAccessNode (like $this->id) for string coercion
            bool isVariableOrAccess = part is VariableNode || part is ObjectAccessNode;
            EmitStringCoercion(part.AnalysedType, isVariable: isVariableOrAccess);
            
            // Store in array
            Emit(OpCodes.Stelem_Ref);
        }

        // Call string.Concat(string[])
        var concatMethod = typeof(string).GetMethod("Concat", new[] { typeof(string[]) })!;
        Emit(OpCodes.Call, concatMethod);
    }
}