using System;
using System.Reflection.Emit;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;
using PHPIL.Engine.Runtime;
using PHPIL.Engine.Runtime.Types;
using System.Collections.Generic;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class FunctionCallNode
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

            string funcName = Callee switch
            {
                LiteralNode lit => lit.Token.TextValue(source),
                _ => throw new Exception("Unsupported callee type for function call.")
            };

            il.Emit(OpCodes.Ldsfld, typeof(GlobalRuntimeContext).GetField("FunctionTable")!);
            il.Emit(OpCodes.Ldstr, funcName);
            il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, PhpFunction>).GetMethod("get_Item")!);
            il.Emit(OpCodes.Ldfld, typeof(PhpFunction).GetField("Action")!);

            il.Emit(OpCodes.Ldc_I4, Args.Count);
            il.Emit(OpCodes.Newarr, typeof(PhpValue));

            for (int i = 0; i < Args.Count; i++)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4, i);

                ilProducer.Visit(Args[i], in source);

                if (ilProducer.LastEmittedType != typeof(PhpValue))
                {
                    if (ilProducer.LastEmittedType != null && ilProducer.LastEmittedType.IsValueType)
                        il.Emit(OpCodes.Box, ilProducer.LastEmittedType);

                    il.Emit(OpCodes.Newobj, typeof(PhpValue).GetConstructor(new Type[] { typeof(object) })!);
                }

                il.Emit(OpCodes.Stelem_Ref);
                ilProducer.LastEmittedType = typeof(PhpValue);
            }

            il.Emit(OpCodes.Callvirt, typeof(PhpCallable).GetMethod("Invoke")!);

            if (GlobalRuntimeContext.FunctionTable.TryGetValue(funcName, out var phpFunc) &&
                phpFunc.ReturnType != null && !phpFunc.ReturnType.IsVoid)
            {
                ilProducer.LastEmittedType = typeof(object);
            }
            else
            {
                il.Emit(OpCodes.Pop);
                ilProducer.LastEmittedType = null;
            }
        }
    }
}