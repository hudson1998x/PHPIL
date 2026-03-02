using System;
using System.Reflection.Emit;
using PHPIL.Engine.Runtime;
using PHPIL.Engine.Runtime.Types;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;

namespace PHPIL.Engine.SyntaxTree
{
    /// <summary>
    /// Visitor implementation for <see cref="FunctionNode"/> — compiles a named PHP
    /// function declaration into a <see cref="DynamicMethod"/> and registers it in
    /// the global function table so it can be called at runtime.
    ///
    /// <para>
    /// Each function gets its own isolated <see cref="DynamicMethod"/> and a fresh
    /// <see cref="IlProducer"/> instance, completely separate from the top-level
    /// script's method. This means function bodies are compiled independently —
    /// locals, scope frames, and <c>LastEmittedType</c> state don't bleed between
    /// the function and its call site. The compiled method is then wrapped in a
    /// <see cref="PhpCallable"/> delegate and stored in
    /// <see cref="GlobalRuntimeContext.FunctionTable"/> so call sites can look it
    /// up by name without any IL-level linking.
    /// </para>
    /// </summary>
    public partial class FunctionNode
    {
        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            // Route to the typed IL emission path if the visitor is an IlProducer,
            // otherwise fall through to the base SyntaxNode.Accept so other visitor
            // types (analysers, pretty-printers) still traverse the node correctly.
            if (visitor is IlProducer ilProducer)
            {
                Accept(ilProducer, in source);
                return;
            }

            base.Accept(visitor, in source);
        }

        /// <summary>
        /// Typed IL emission path — compiles the function body and registers the result.
        /// </summary>
        private void Accept(IlProducer ilProducer, in ReadOnlySpan<char> source)
        {
            string funcName = Name.TextValue(source);

            // Create a new DynamicMethod for this function. All PHP functions share
            // the same signature: they receive their arguments as a PhpValue[] array
            // and return a single object (either a PhpValue or null for void functions).
            // Using GlobalRuntimeContext's module as the owner gives the method access
            // to internal types in that assembly — necessary for calling runtime helpers.
            var dynMethod = new DynamicMethod(
                $"phpil_{funcName}",           // prefixed to avoid name collisions with CLR internals
                typeof(object),
                new Type[] { typeof(PhpValue[]) },
                typeof(GlobalRuntimeContext).Module);

            ILGenerator ilGen = dynMethod.GetILGenerator();

            // Create an isolated IlProducer for the function body. Using a separate
            // producer rather than the caller's ensures that the function's locals,
            // scope frames, and LastEmittedType don't interfere with whatever the
            // top-level script is currently compiling.
            var funcProducer = new IlProducer(ilGen);

            // Push a scope frame marked as BreaksTraversal so that variable lookups
            // inside the function don't walk up into the enclosing script scope.
            // PHP functions have no closure over the calling scope by default — only
            // explicitly `use`d variables are captured, which isn't implemented here yet.
            var frame = new StackFrame { BreaksTraversal = true };
            funcProducer.GetContext().PushFrame(frame);

            // ── Parameter unpacking ───────────────────────────────────────────
            // The DynamicMethod receives all arguments as a single PhpValue[] (Ldarg_0).
            // For each declared parameter, emit IL that extracts element [i] from the
            // array and stores it into a dedicated local, then registers the name→slot
            // mapping so the body can reference it by name via VariableNode.
            for (int i = 0; i < Params.Count; i++)
            {
                string paramName = Params[i].Name.TextValue(source);

                var local = ilGen.DeclareLocal(typeof(PhpValue));

                ilGen.Emit(OpCodes.Ldarg_0);       // push the PhpValue[] args array
                ilGen.Emit(OpCodes.Ldc_I4, i);     // push the parameter index
                ilGen.Emit(OpCodes.Ldelem_Ref);    // pop array + index, push args[i]
                ilGen.Emit(OpCodes.Stloc, local);  // store into the parameter local

                frame.RegisterVariable(paramName, local.LocalIndex);
            }

            // ── Body compilation ──────────────────────────────────────────────
            // Visit the body block through the function's own producer. All statement
            // and expression nodes inside the body emit into ilGen, not the caller's generator.
            if (Body != null)
                Body.Accept(funcProducer, in source);

            // Emit a fallthrough return of PhpValue.Void for functions that don't
            // have an explicit `return` statement. Without this, execution would fall
            // off the end of the method and cause a runtime error.
            ilGen.Emit(OpCodes.Ldsfld, typeof(PhpValue).GetField("Void")!);
            ilGen.Emit(OpCodes.Ret);

            funcProducer.GetContext().PopFrame();

            // ── Registration ──────────────────────────────────────────────────
            // Wrap the compiled DynamicMethod in a PhpCallable delegate. The lambda
            // captures dynMethod by reference — each function declaration closes over
            // its own DynamicMethod so multiple functions don't interfere with each other.
            PhpCallable callable = args =>
            {
                return (PhpValue)dynMethod.Invoke(null, [args])!;
            };

            // Resolve the declared return type annotation, defaulting to PhpValue.Void
            // for unannotated or explicitly `void` functions. This is stored on the
            // PhpFunction for runtime type checking rather than being enforced here at
            // compile time.
            var nodeReturnType    = ReturnType;
            var returnTypeValue   = (nodeReturnType == null || nodeReturnType.Value.TextValue(source) == "void")
                ? PhpValue.Void
                : new PhpValue(nodeReturnType.Value.TextValue(source));

            var phpFunc = new PhpFunction
            {
                Name       = funcName,
                Action     = callable,
                IsSystem   = false,
                IsCompiled = true,
                ReturnType = returnTypeValue
            };

            // Register in the global function table so call sites can resolve the
            // function by name at runtime. Last-write wins — re-declaring a function
            // replaces the previous definition, matching PHP's runtime behaviour.
            GlobalRuntimeContext.FunctionTable[funcName] = phpFunc;

            // Function declarations don't leave a value on the evaluation stack —
            // they're statements, not expressions. Clear LastEmittedType so the
            // parent node doesn't mistakenly believe a value was produced.
            ilProducer.LastEmittedType = null;
        }
    }
}