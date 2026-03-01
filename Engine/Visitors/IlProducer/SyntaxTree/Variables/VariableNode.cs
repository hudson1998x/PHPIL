using System;
using System.Reflection.Emit;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;

namespace PHPIL.Engine.SyntaxTree;

public partial class VariableNode
{
    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
    {
        if (visitor is not IlProducer ilProducer) return;
        var il = ilProducer.GetILGenerator();

        // Load local variable (already PhpValue!)
        if (ilProducer.GetContext().TryGetVariableSlot(Token.TextValue(in source), out var localIndex))
        {
            il.Emit(OpCodes.Ldloc, localIndex);   
        }
        else
        {
            throw new Exception("Variable not found");
        }

        ilProducer.LastEmittedType = typeof(PHPIL.Engine.Runtime.Types.PhpValue);
    }
}