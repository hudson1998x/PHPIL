using System;
using System.Reflection.Emit;
using PHPIL.Engine.Runtime;
using PHPIL.Engine.Runtime.Types;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class AnonymousFunctionNode
    {
        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            if (visitor is IlProducer ilProducer)
            {
                Accept(ilProducer, in source);
                return;
            }

            base.Accept(visitor, in source);
        }

        private void Accept(IlProducer ilProducer, in ReadOnlySpan<char> source)
        {
            var il = ilProducer.GetILGenerator();

            var dynMethod = new DynamicMethod(
                $"phpil_closure_{Guid.NewGuid():N}",
                typeof(object),
                new Type[] { typeof(PhpValue[]) },
                typeof(GlobalRuntimeContext).Module);

            ILGenerator ilGen = dynMethod.GetILGenerator();
            var funcProducer  = new IlProducer(ilGen);

            var frame = new StackFrame { BreaksTraversal = true };
            funcProducer.GetContext().PushFrame(frame);

            for (int i = 0; i < Params.Count; i++)
            {
                string paramName = Params[i].Name.TextValue(source);
                var local        = ilGen.DeclareLocal(typeof(PhpValue));

                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldc_I4, i);
                ilGen.Emit(OpCodes.Ldelem_Ref);
                ilGen.Emit(OpCodes.Stloc, local);

                frame.RegisterVariable(paramName, local.LocalIndex);
            }

            if (Body != null)
                Body.Accept(funcProducer, in source);

            ilGen.Emit(OpCodes.Ldsfld, typeof(PhpValue).GetField("Void")!);
            ilGen.Emit(OpCodes.Ret);

            funcProducer.GetContext().PopFrame();

            // ── Park the closure in GlobalRuntimeContext.Constants ────────────
            // DynamicMethod IL can't embed arbitrary object references as operands,
            // so we hand the PhpValue off via the static Constants list and emit an
            // index load — same pattern as any other compile→runtime hand-off.
            PhpCallable callable = args => (PhpValue)dynMethod.Invoke(null, [args])!;
            var callableValue    = new PhpValue(callable);

            GlobalRuntimeContext.Constants.Add(callableValue);
            int constantIndex = GlobalRuntimeContext.Constants.Count - 1;

            il.Emit(OpCodes.Ldsfld, typeof(GlobalRuntimeContext).GetField("Constants")!);
            il.Emit(OpCodes.Ldc_I4, constantIndex);
            il.Emit(OpCodes.Call,   typeof(List<PhpValue>).GetMethod("get_Item")!);
            // Stack: [ PhpValue(PhpCallable) ]

            ilProducer.LastEmittedType = typeof(PhpValue);
        }
    }
}