using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree.Structure.Loops;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Emits IL for a <c>foreach</c> loop, iterating over any PHP-compatible iterable using
    /// the <c>Runtime.Sdk.Enumerable</c> abstraction.
    /// </summary>
    /// <param name="node">The <see cref="ForeachNode"/> representing the foreach loop.</param>
    /// <param name="source">The original source text, used to resolve the key and value variable names.</param>
    /// <exception cref="Exception">Thrown when the foreach construct has no value variable.</exception>
    /// <remarks>
    /// <para>
    /// The iterable expression is evaluated and passed to <c>Enumerable.GetEnumerator</c>, which
    /// returns an opaque enumerator object stored in a temporary local. The enumerator is
    /// compatible with both PHP arrays and <c>Iterator</c> objects.
    /// </para>
    /// <para>
    /// Value (and optionally key) variables are declared as <see cref="object"/> locals and
    /// registered in <c>_locals</c>. Loop control labels are pushed onto <c>_breakLabels</c>
    /// and <c>_continueLabels</c> before the body is emitted and popped afterwards, so that
    /// nested <c>break</c> and <c>continue</c> statements resolve correctly.
    /// </para>
    /// <para>
    /// The emitted loop structure is:
    /// </para>
    /// <list type="number">
    ///   <item><description>Branch unconditionally to <c>loopStart</c>.</description></item>
    ///   <item><description>Mark <c>loopStart</c>; call <c>Enumerable.MoveNext</c> — branch to <c>loopEnd</c> if it returns <see langword="false"/>.</description></item>
    ///   <item><description>Load the current value via <c>Enumerable.GetCurrent</c> into the value local.</description></item>
    ///   <item><description>If a key variable is declared, load the current key via <c>Enumerable.GetKey</c> into the key local.</description></item>
    ///   <item><description>Emit the loop body, then branch back to <c>loopStart</c>.</description></item>
    ///   <item><description>Mark <c>loopEnd</c>.</description></item>
    /// </list>
    /// </remarks>
    public void VisitForeachNode(ForeachNode node, in ReadOnlySpan<char> source)
    {
        // Get the array expression on stack
        node.Iterable?.Accept(this, source);
        
        // Get enumerator (works for arrays and Iterator objects)
        var getEnumerator = typeof(PHPIL.Engine.Runtime.Sdk.Enumerable).GetMethod("GetEnumerator")!;
        Emit(OpCodes.Call, getEnumerator);
        
        var valueVar = node.Value?.Token.TextValue(in source);
        var keyVar = node.Key?.Token.TextValue(in source);
        
        if (valueVar == null) throw new Exception("Foreach requires a value variable");
        
        // Store enumerator
        var enumeratorLocal = DeclareLocal(typeof(object));
        Emit(OpCodes.Stloc, enumeratorLocal);
        
        // Declare value variable
        var valueLocal = DeclareLocal(typeof(object));
        _locals[valueVar] = valueLocal;
        
        LocalBuilder? keyLocal = null;
        if (keyVar != null)
        {
            keyLocal = DeclareLocal(typeof(object));
            _locals[keyVar] = keyLocal;
        }
        
        // Create labels
        var loopStart = DefineLabel();
        var loopEnd = DefineLabel();
        
        // Push break/continue labels
        _breakLabels.Push(loopEnd);
        _continueLabels.Push(loopStart);
        
        // Start loop
        Emit(OpCodes.Br, loopStart);
        MarkLabel(loopStart);
        
        // Call MoveNext on enumerator
        var moveNext = typeof(PHPIL.Engine.Runtime.Sdk.Enumerable).GetMethod("MoveNext")!;
        Emit(OpCodes.Ldloc, enumeratorLocal);
        Emit(OpCodes.Call, moveNext);
        Emit(OpCodes.Brfalse, loopEnd);
        
        // Get current value
        var getCurrent = typeof(PHPIL.Engine.Runtime.Sdk.Enumerable).GetMethod("GetCurrent")!;
        Emit(OpCodes.Ldloc, enumeratorLocal);
        Emit(OpCodes.Call, getCurrent);
        Emit(OpCodes.Stloc, valueLocal);
        
        // Get key if needed
        if (keyVar != null && keyLocal != null)
        {
            var getKey = typeof(PHPIL.Engine.Runtime.Sdk.Enumerable).GetMethod("GetKey")!;
            Emit(OpCodes.Ldloc, enumeratorLocal);
            Emit(OpCodes.Call, getKey);
            Emit(OpCodes.Stloc, keyLocal);
        }
        
        // Execute body
        node.Body?.Accept(this, source);
        
        // Loop back
        Emit(OpCodes.Br, loopStart);
        
        MarkLabel(loopEnd);
        
        // Pop labels
        _breakLabels.Pop();
        _continueLabels.Pop();
    }
}