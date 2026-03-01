using System;
using System.Reflection.Emit;
using PHPIL.Engine.Runtime;
using PHPIL.Engine.Runtime.Types;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class FunctionNode
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
            string funcName = Name.TextValue(source);

            var dynMethod = new DynamicMethod(
                $"phpil_{funcName}",
                typeof(object),
                new Type[] { typeof(PhpValue[]) },
                typeof(GlobalRuntimeContext).Module);

            ILGenerator ilGen = dynMethod.GetILGenerator();

            var funcProducer = new IlProducer(ilGen);

            var frame = new StackFrame { BreaksTraversal = true };
            funcProducer.GetContext().PushFrame(frame);

            for (int i = 0; i < Params.Count; i++)
            {
                string paramName = Params[i].Name.TextValue(source);

                var local = ilGen.DeclareLocal(typeof(PhpValue));

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

            PhpCallable callable = args =>
            {
                return (PhpValue)dynMethod.Invoke(null, [args])!;
            };

            var nodeReturnType = ReturnType;
            var returnTypeValue = (nodeReturnType == null || nodeReturnType.Value.TextValue(source) == "void")
                ? PhpValue.Void
                : new PhpValue(nodeReturnType.Value.TextValue(source));

            var phpFunc = new PhpFunction
            {
                Name = funcName,
                Action = callable,
                IsSystem = false,
                IsCompiled = true,
                ReturnType = returnTypeValue
            };

            GlobalRuntimeContext.FunctionTable[funcName] = phpFunc;

            ilProducer.LastEmittedType = null;
        }
    }
}