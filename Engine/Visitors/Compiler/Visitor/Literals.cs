using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.OOP;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Emits IL to push a literal value onto the stack.
    /// </summary>
    /// <param name="node">The <see cref="LiteralNode"/> representing the literal value.</param>
    /// <param name="source">The original source text, used to extract the literal's text value.</param>
    /// <exception cref="NotImplementedException">Thrown for unrecognised literal token kinds.</exception>
    /// <remarks>
    /// Each literal kind maps to a single IL instruction: <c>true</c> and <c>false</c> push
    /// <c>1</c> and <c>0</c> as <see cref="int"/> via <see cref="OpCodes.Ldc_I4_1"/> and
    /// <see cref="OpCodes.Ldc_I4_0"/>; string literals have their surrounding quotes stripped
    /// before being pushed via <see cref="OpCodes.Ldstr"/>; <c>null</c> pushes
    /// <see langword="null"/> via <see cref="OpCodes.Ldnull"/>; integer and float literals are
    /// parsed and pushed via <see cref="OpCodes.Ldc_I4"/> and <see cref="OpCodes.Ldc_R8"/>
    /// respectively. No boxing is emitted here — callers are responsible for boxing where needed.
    /// </remarks>
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

    /// <summary>
    /// Emits IL to evaluate a PHP double-quoted interpolated string, producing a single
    /// concatenated <see cref="string"/> on the stack.
    /// </summary>
    /// <param name="node">The <see cref="InterpolatedStringNode"/> containing the string parts.</param>
    /// <param name="source">The original source text, passed through to child part visitors.</param>
    /// <remarks>
    /// <para>
    /// If the node has no parts, an empty string is pushed directly via
    /// <see cref="OpCodes.Ldstr"/>.
    /// </para>
    /// <para>
    /// Otherwise, a <c>string[]</c> sized to the part count is allocated. Each part is visited
    /// in order, coerced to <see cref="string"/> via <c>EmitStringCoercion</c> (with
    /// <c>isVariable: true</c> for <see cref="VariableNode"/> and <see cref="ObjectAccessNode"/>
    /// parts), and stored into the array via <see cref="OpCodes.Stelem_Ref"/>. Finally,
    /// <c>string.Concat(string[])</c> is called to produce the result.
    /// </para>
    /// </remarks>
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