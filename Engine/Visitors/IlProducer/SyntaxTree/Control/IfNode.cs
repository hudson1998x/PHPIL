using System.Reflection.Emit;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;
using PHPIL.Engine.Runtime.Types;
using System.Collections.Generic;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class IfNode : SyntaxNode
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

            var endLabel = il.DefineLabel();
            var elseLabel = il.DefineLabel();

            if (Expression != null)
            {
                ilProducer.Visit(Expression, in source);

                if (ilProducer.LastEmittedType != typeof(PhpValue))
                {
                    if (ilProducer.LastEmittedType != null && ilProducer.LastEmittedType.IsValueType)
                        il.Emit(OpCodes.Box, ilProducer.LastEmittedType);

                    il.Emit(OpCodes.Newobj, typeof(PhpValue).GetConstructor(new[] { typeof(object) })!);
                }

                il.Emit(OpCodes.Callvirt, typeof(PhpValue).GetMethod("ToBool")!);
                il.Emit(OpCodes.Brfalse, elseLabel);
            }

            Body?.Accept(ilProducer, in source);

            il.Emit(OpCodes.Br, endLabel);

            il.MarkLabel(elseLabel);
            foreach (var elseif in ElseIfs)
                elseif.Accept(ilProducer, in source);

            ElseNode?.Accept(ilProducer, in source);

            il.MarkLabel(endLabel);

            ilProducer.LastEmittedType = typeof(object);
        }
    }
}