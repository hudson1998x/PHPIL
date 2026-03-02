using System;
using System.Reflection;
using System.Reflection.Emit;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;
using PHPIL.Engine.Runtime;
using PHPIL.Engine.Runtime.Types;
using System.Collections.Generic;

namespace PHPIL.Engine.SyntaxTree
{
    /// <summary>
    /// Visitor implementation for <see cref="FunctionCallNode"/> — emits IL that
    /// resolves a named PHP function from the global function table at runtime,
    /// evaluates and packs its arguments into a <see cref="PhpValue"/> array, and
    /// invokes the function's compiled delegate.
    ///
    /// <para>
    /// Function resolution is intentionally deferred to runtime rather than resolved
    /// at compile time. PHP allows functions to be declared after their first call
    /// site (forward references), and the global function table may be populated by
    /// code that runs before this call is reached — so looking up by name at the
    /// point of invocation is the only approach that handles all cases correctly.
    /// </para>
    /// </summary>
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

        /// <summary>
        /// Typed IL emission path. Emits the full call sequence:
        /// resolution → argument array construction → delegate invocation.
        ///
        /// <para>
        /// All three callee paths (variable, IIFE, named) always leave a
        /// <see cref="PhpValue"/> on the evaluation stack after invocation.
        /// It is the responsibility of the parent node — typically
        /// <see cref="BlockNode"/> at statement level — to pop the value
        /// if it is unused. This keeps nested calls like
        /// <c>var_dump(strlen("x"))</c> working correctly without any
        /// compile-time knowledge of return types.
        /// </para>
        /// </summary>
        private void Accept(IlProducer ilProducer, in ReadOnlySpan<char> source)
        {
            var il = ilProducer.GetILGenerator();

            if (Callee is VariableNode varCallee)
            {
                // ── Variable callee (closure or string function name) ──────────
                // Rather than a compile-time table lookup, we load the PhpValue from
                // the local slot and delegate resolution to InvokeVariable at runtime,
                // which handles both PhpCallable closures and string-named functions.
                string varName = varCallee.Token.TextValue(source);

                if (!ilProducer.GetContext().TryGetVariableSlot(varName, out int slot))
                    throw new Exception($"Undefined variable '{varName}'");

                il.Emit(OpCodes.Ldloc, slot);
                // Stack: [ PhpValue ]

                EmitArgArray(ilProducer, il, source);
                // Stack: [ PhpValue, PhpValue[] ]

                il.Emit(OpCodes.Call, typeof(FunctionCallNode).GetMethod(
                    nameof(InvokeVariable),
                    BindingFlags.NonPublic | BindingFlags.Static)!);
                // Stack: [ PhpValue ]
            }
            else if (Callee is GroupNode { Inner: AnonymousFunctionNode }
                     || Callee is AnonymousFunctionNode)
            {
                // ── Immediately-invoked anonymous function: `(function() { ... })()` ──
                // Visit the callee — AnonymousFunctionNode.Accept leaves a PhpValue(PhpCallable)
                // on the stack, which InvokeVariable can invoke directly.
                ilProducer.Visit(Callee, in source);
                // Stack: [ PhpValue ]

                EmitArgArray(ilProducer, il, source);
                // Stack: [ PhpValue, PhpValue[] ]

                il.Emit(OpCodes.Call, typeof(FunctionCallNode).GetMethod(
                    nameof(InvokeVariable),
                    BindingFlags.NonPublic | BindingFlags.Static)!);
                // Stack: [ PhpValue ]
            }
            else
            {
                // ── Named callee — runtime function table lookup ───────────────
                // Resolution is deferred entirely to runtime via ResolveNamed, which
                // throws a clear "undefined function" error if the name isn't registered.
                // This replaces the previous compile-time TryGetValue check, which
                // caused stack corruption when the function wasn't in the table yet.
                string funcName = Callee switch
                {
                    LiteralNode lit => lit.Token.TextValue(source),
                    _ => throw new Exception("Unsupported callee type for function call.")
                };

                il.Emit(OpCodes.Ldstr, funcName);
                il.Emit(OpCodes.Call,  typeof(FunctionCallNode).GetMethod(
                    nameof(ResolveNamed),
                    BindingFlags.NonPublic | BindingFlags.Static)!);
                // Stack: [ PhpCallable ]

                EmitArgArray(ilProducer, il, source);
                // Stack: [ PhpCallable, PhpValue[] ]

                il.Emit(OpCodes.Callvirt, typeof(PhpCallable).GetMethod("Invoke")!);
                // Stack: [ PhpValue ]
            }

            // Always leave the PhpValue return on the stack — the parent node
            // decides whether to consume or discard it. BlockNode.Accept emits
            // Pop for statement-level calls; expression contexts consume it directly.
            ilProducer.LastEmittedType = typeof(PhpValue);
        }

        /// <summary>
        /// Emits IL to build the <see cref="PhpValue"/>[] argument array for a call.
        /// Extracted to avoid duplicating the same loop across all three callee paths.
        /// </summary>
        private void EmitArgArray(IlProducer ilProducer, ILSpy il, in ReadOnlySpan<char> source)
        {
            il.Emit(OpCodes.Ldc_I4, Args.Count);
            il.Emit(OpCodes.Newarr, typeof(PhpValue));

            for (int i = 0; i < Args.Count; i++)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4, i);

                ilProducer.Visit(Args[i], in source);

                // Normalise the argument to PhpValue if the expression emitted
                // something else. Value types need boxing first since PhpValue's
                // constructor takes `object`. In practice most expressions already
                // produce PhpValue, so this branch is rarely taken.
                if (ilProducer.LastEmittedType != typeof(PhpValue))
                {
                    if (ilProducer.LastEmittedType != null && ilProducer.LastEmittedType.IsValueType)
                        il.Emit(OpCodes.Box, ilProducer.LastEmittedType);

                    il.Emit(OpCodes.Newobj, typeof(PhpValue).GetConstructor(new Type[] { typeof(object) })!);
                }

                il.Emit(OpCodes.Stelem_Ref);                       // args[i] = value
                ilProducer.LastEmittedType = typeof(PhpValue);     // keep tracker consistent
            }
        }

        /// <summary>
        /// Resolves a named function from the global function table at runtime.
        /// Throws a clear <see cref="InvalidOperationException"/> if the function
        /// has not been registered, rather than allowing a <see cref="KeyNotFoundException"/>
        /// to surface from the dictionary.
        /// </summary>
        internal static PhpCallable ResolveNamed(string funcName)
        {
            if (GlobalRuntimeContext.FunctionTable.TryGetValue(funcName, out var phpFunc))
                return phpFunc.Action;
            throw new InvalidOperationException($"Call to undefined function '{funcName}'");
        }

        /// <summary>
        /// Runtime resolver for variable callees. Handles both closure and string cases:
        /// <list type="bullet">
        ///   <item>A <see cref="PhpCallable"/> stored in the variable is invoked directly.</item>
        ///   <item>A <see cref="string"/> is treated as a named function and looked up in
        ///   <see cref="GlobalRuntimeContext.FunctionTable"/> at the point of invocation.</item>
        /// </list>
        /// </summary>
        internal static PhpValue InvokeVariable(PhpValue callee, PhpValue[] args)
        {
            switch (callee.Value)
            {
                case PhpCallable callable:
                    return new PhpValue(callable(args));

                case string funcName:
                    if (GlobalRuntimeContext.FunctionTable.TryGetValue(funcName, out var phpFunc))
                        return new PhpValue(phpFunc.Action(args));
                    throw new InvalidOperationException($"Call to undefined function '{funcName}'");

                default:
                    throw new InvalidOperationException(
                        $"Value of type '{callee.Value?.GetType().Name ?? "null"}' is not callable");
            }
        }
    }
}