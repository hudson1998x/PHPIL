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
        /// table lookup → argument array construction → delegate invocation → return handling.
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

                // ── Build the PhpValue[] argument array ───────────────────────
                il.Emit(OpCodes.Ldc_I4, Args.Count);
                il.Emit(OpCodes.Newarr, typeof(PhpValue));
                // Stack: [ PhpValue, PhpValue[] ]

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
                // Stack: [ PhpValue, PhpValue[] ]

                // InvokeVariable handles both PhpCallable and string at runtime —
                // no global table lookup needed at the call site.
                il.Emit(OpCodes.Call, typeof(FunctionCallNode).GetMethod(
                    nameof(InvokeVariable),
                    BindingFlags.NonPublic | BindingFlags.Static)!);
                // Stack: [ PhpValue ]

                il.Emit(OpCodes.Pop);
                ilProducer.LastEmittedType = null;
            }
            else
            {
                // ── Named callee — function table lookup ──────────────────────
                // Emit a runtime dictionary lookup: GlobalRuntimeContext.FunctionTable[funcName].Action
                // This is a late-bound lookup — the function doesn't need to exist at compile
                // time, only at the moment this IL executes. Handles forward references and
                // dynamically declared functions naturally.
                string funcName = Callee switch
                {
                    LiteralNode lit => lit.Token.TextValue(source),
                    _ => throw new Exception("Unsupported callee type for function call.")
                };

                il.Emit(OpCodes.Ldsfld,   typeof(GlobalRuntimeContext).GetField("FunctionTable")!);
                il.Emit(OpCodes.Ldstr,    funcName);
                il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, PhpFunction>).GetMethod("get_Item")!);
                il.Emit(OpCodes.Ldfld,    typeof(PhpFunction).GetField("Action")!);
                // Stack: [ PhpCallable ]

                // ── Build the PhpValue[] argument array ───────────────────────
                // All functions share the same calling convention: a single PhpValue[] parameter.
                // The array is built on the stack element by element using Dup to keep the
                // array reference available for each Stelem_Ref without reloading it.
                il.Emit(OpCodes.Ldc_I4, Args.Count);
                il.Emit(OpCodes.Newarr, typeof(PhpValue));
                // Stack: [ PhpCallable, PhpValue[] ]

                for (int i = 0; i < Args.Count; i++)
                {
                    il.Emit(OpCodes.Dup);       // keep the array reference on the stack
                    il.Emit(OpCodes.Ldc_I4, i); // element index
                    // Stack: [ PhpCallable, PhpValue[], PhpValue[], i ]

                    // Visit the argument expression — leaves a value on the stack.
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

                    il.Emit(OpCodes.Stelem_Ref);                        // args[i] = value
                    ilProducer.LastEmittedType = typeof(PhpValue);      // keep tracker consistent
                }
                // Stack: [ PhpCallable, PhpValue[] ]

                // ── Invoke the delegate ───────────────────────────────────────
                // Callvirt on the delegate's Invoke method passes the args array as the
                // single argument, matching the PhpCallable signature defined in FunctionNode.
                il.Emit(OpCodes.Callvirt, typeof(PhpCallable).GetMethod("Invoke")!);
                // Stack: [ object (return value) ]

                // ── Handle the return value ───────────────────────────────────
                // Check the function's declared return type at compile time to decide
                // whether the return value should be kept on the stack or discarded.
                // Note: this is a compile-time check against the function table — if the
                // function was declared after this call site, the table entry may not exist
                // yet and the fallback (Pop) is taken. This could be revisited with a
                // two-pass compilation strategy.
                if (GlobalRuntimeContext.FunctionTable.TryGetValue(funcName, out var phpFunc) &&
                    phpFunc.ReturnType != null && !phpFunc.ReturnType.IsVoid)
                {
                    // Non-void return — leave the value on the stack for the parent node.
                    ilProducer.LastEmittedType = typeof(object);
                }
                else
                {
                    // Void return (or unknown function) — discard the return value. All
                    // delegates return `object` by signature, so there's always something
                    // to pop even for void functions. Without this Pop the stack would be
                    // unbalanced and the runtime would reject the method.
                    il.Emit(OpCodes.Pop);
                    ilProducer.LastEmittedType = null;
                }
            }
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