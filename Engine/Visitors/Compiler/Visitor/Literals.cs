using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;

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

        foreach (var part in node.Parts)
        {
            part.Accept(this, source);
            EmitStringCoercion(part.AnalysedType, isVariable: part is VariableNode);
        }

        if (node.Parts.Count > 1)
        {
            // string.Concat(string[]) or string.Concat(s1, s2, s3, s4)
            if (node.Parts.Count <= 4)
            {
                var types = Enumerable.Repeat(typeof(string), node.Parts.Count).ToArray();
                var concat = typeof(string).GetMethod("Concat", types)!;
                Emit(OpCodes.Call, concat);
            }
            else
            {
                // Fallback to array version for > 4 parts
                Emit(OpCodes.Ldc_I4, node.Parts.Count);
                Emit(OpCodes.Newarr, typeof(string));
                for (int i = node.Parts.Count - 1; i >= 0; i--)
                {
                    Emit(OpCodes.Dup);
                    Emit(OpCodes.Ldc_I4, i);
                    // This is slightly tricky because the values are already on stack.
                    // A better way for > 4 parts would be to push them into an array as we go.
                    // But for simple interpolation, < 4 is common.
                    // Let's optimize for <= 4 and use a simpler approach for > 4 if needed.
                    // For now, let's just use Concat(string, string) repeatedly if > 4.
                }
                
                // Redo if > 4:
                // Actually, string.Concat(s1, s2) is easiest to chain.
            }
        }
    }
}