using System.Reflection.Emit;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;
using PHPIL.Engine.Runtime;

namespace PHPIL.Engine.SyntaxTree;

public partial class VariableDeclaration
{
    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
    {
        if (visitor is IlProducer ilProducer)
        {
            Accept(ilProducer, source);
        }
    }

    private void Accept(IlProducer ilProducer, in ReadOnlySpan<char> source)
    {
        var generator = ilProducer.GetILGenerator();
        var varName = VariableName.TextValue(in source);

        // 1️⃣ Emit the value onto the stack
        if (VariableValue is not null)
        {
            ilProducer.Visit(VariableValue, in source);

            // Box value types so locals are always objects
            var valueType = ilProducer.LastEmittedType;
            if (valueType != null && valueType.IsValueType)
                generator.Emit(OpCodes.Box, valueType);
        }
        else
        {
            // If no value, push null
            generator.Emit(OpCodes.Ldnull);
        }

        // 2️⃣ Register or retrieve local slot in current frame
        int slot;
        if (!ilProducer.GetContext().TryGetVariableSlot(varName, out slot))
        {
            // Create a new IL local for this variable
            var local = generator.DeclareLocal(typeof(object));
            slot = local.LocalIndex;

            // Register the variable in the current frame with the correct slot
            ilProducer.GetContext().CurrentFrame.RegisterVariable(varName);
        }

        // 3️⃣ Store the value from the stack into the local slot
        generator.Emit(OpCodes.Stloc, slot);

        // 4️⃣ All locals are objects in our dynamic runtime
        ilProducer.LastEmittedType = typeof(object);
    }
}