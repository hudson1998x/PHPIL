using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree.Structure.Loops;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
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