using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace PHPIL.Engine.Visitors.IlProducer
{
    /// <summary>
    /// A transparent wrapper around <see cref="ILGenerator"/> that optionally
    /// records every emitted instruction, label, and local declaration to an
    /// in-memory log for debugging purposes.
    ///
    /// <para>
    /// All method calls pass straight through to the underlying
    /// <see cref="ILGenerator"/> — behaviour is identical whether logging is
    /// enabled or not. When <paramref name="enabled"/> is <c>true</c>, each
    /// call also appends a human-readable representation of the instruction to
    /// an internal <see cref="StringBuilder"/>, retrievable via <see cref="GetLog"/>.
    /// </para>
    ///
    /// <para>
    /// The logging overhead is gated behind <c>_enabled</c> so there is zero cost
    /// in production builds — no string allocations, no <see cref="StringBuilder"/>
    /// appends — while still allowing the full IL stream to be inspected during
    /// development without attaching an external IL disassembler.
    /// </para>
    /// </summary>
    public class ILSpy
    {
        private readonly ILGenerator _il;
        private readonly StringBuilder _log = new();

        /// <summary>When false, all logging branches are skipped entirely.</summary>
        private readonly bool _enabled;

        /// <summary>
        /// Initialises the spy around an existing <see cref="ILGenerator"/>.
        /// </summary>
        /// <param name="il">The generator to wrap. Must not be <c>null</c>.</param>
        /// <param name="enabled">
        /// Pass <c>true</c> to activate instruction logging.
        /// Defaults to <c>false</c> so production code pays no logging cost
        /// unless explicitly opted in.
        /// </param>
        public ILSpy(ILGenerator il, bool enabled = false)
        {
            _il = il ?? throw new ArgumentNullException(nameof(il));
            _enabled = enabled;
        }

        // ── Emit overloads ────────────────────────────────────────────────────
        // One overload per argument type mirrors the ILGenerator API exactly,
        // so call sites need no changes when switching from a raw ILGenerator
        // to an ILSpy. Each overload logs a readable representation of the
        // instruction before forwarding to the real generator.

        /// <summary>Emits a zero-operand instruction.</summary>
        public void Emit(OpCode opcode)
        {
            if (_enabled)
                _log.AppendLine(opcode.ToString());
            _il.Emit(opcode);
        }

        /// <summary>Emits an instruction with an integer operand (e.g. <c>ldc.i4</c>).</summary>
        public void Emit(OpCode opcode, int arg)
        {
            if (_enabled)
                _log.AppendLine($"{opcode} {arg}");

            _il.Emit(opcode, arg);
        }

        /// <summary>Emits an instruction with a double operand (e.g. <c>ldc.r8</c>).</summary>
        public void Emit(OpCode opcode, double arg)
        {
            if (_enabled)
                _log.AppendLine($"{opcode} {arg}");

            _il.Emit(opcode, arg);
        }

        /// <summary>
        /// Emits an instruction targeting a local variable (e.g. <c>ldloc</c>, <c>stloc</c>).
        /// Logs the local's index rather than its opaque <see cref="LocalBuilder"/> reference
        /// so the output stays human-readable.
        /// </summary>
        public void Emit(OpCode opcode, LocalBuilder local)
        {
            if (_enabled)
                _log.AppendLine($"{opcode} local_{local.LocalIndex}");

            _il.Emit(opcode, local);
        }

        /// <summary>Emits a constructor call (e.g. <c>newobj</c>).</summary>
        public void Emit(OpCode opCode, ConstructorInfo constructor)
        {
            if (_enabled)
                _log.AppendLine($"{opCode} {constructor}");
            _il.Emit(opCode, constructor);
        }

        /// <summary>Emits a field access instruction (e.g. <c>ldfld</c>, <c>stfld</c>).</summary>
        public void Emit(OpCode opCode, FieldInfo fieldInfo)
        {
            if (_enabled)
                _log.AppendLine($"{opCode} {fieldInfo}");
            _il.Emit(opCode, fieldInfo);
        }

        /// <summary>
        /// Emits a method call instruction (e.g. <c>call</c>, <c>callvirt</c>).
        /// Logs as <c>DeclaringType.MethodName</c> for readability rather than
        /// the full <see cref="MethodInfo"/> signature.
        /// </summary>
        public void Emit(OpCode opcode, MethodInfo method)
        {
            if (_enabled)
                _log.AppendLine($"{opcode} {method.DeclaringType?.Name}.{method.Name}");

            _il.Emit(opcode, method);
        }

        /// <summary>Emits a type instruction (e.g. <c>castclass</c>, <c>isinst</c>, <c>newarr</c>).</summary>
        public void Emit(OpCode opcode, Type type)
        {
            if (_enabled)
                _log.AppendLine($"{opcode} {type.Name}");

            _il.Emit(opcode, type);
        }

        /// <summary>Emits a string literal load (<c>ldstr</c>).</summary>
        public void Emit(OpCode opcode, string str)
        {
            if (_enabled)
                _log.AppendLine($"{opcode} \"{str}\"");

            _il.Emit(opcode, str);
        }

        // ── Labels ────────────────────────────────────────────────────────────
        // Labels are logged using their hash code as a stable identifier —
        // the Label struct itself has no meaningful name, so the hash code is
        // the closest thing to a human-readable ID available at runtime.

        /// <summary>
        /// Defines a new label and logs its identity. The hash code is used as a
        /// surrogate ID to correlate <c>DefineLabel</c>, <c>MarkLabel</c>, and
        /// branch instructions in the log output.
        /// </summary>
        public Label DefineLabel()
        {
            var label = _il.DefineLabel();
            if (_enabled)
                _log.AppendLine($"DefineLabel: {label.GetHashCode()}");
            return label;
        }

        /// <summary>
        /// Marks the current IL position as the target of <paramref name="label"/>
        /// and logs the placement.
        /// </summary>
        public void MarkLabel(Label label)
        {
            if (_enabled)
                _log.AppendLine($"MarkLabel: {label.GetHashCode()}");

            _il.MarkLabel(label);
        }

        // ── Scopes ────────────────────────────────────────────────────────────

        /// <summary>Begins a lexical scope for local variable lifetime tracking.</summary>
        public void BeginScope()
        {
            if (_enabled)
                _log.AppendLine("BeginScope");

            _il.BeginScope();
        }

        /// <summary>Ends the current lexical scope.</summary>
        public void EndScope()
        {
            if (_enabled)
                _log.AppendLine("EndScope");

            _il.EndScope();
        }

        // ── Branching ─────────────────────────────────────────────────────────

        /// <summary>
        /// Emits a branch instruction to a single label (e.g. <c>br</c>, <c>brtrue</c>,
        /// <c>beq</c>). Logs the target using the label's hash code so branch
        /// targets can be matched against their <c>MarkLabel</c> entries in the log.
        /// </summary>
        public void Emit(OpCode opcode, Label label)
        {
            if (_enabled)
                _log.AppendLine($"{opcode} -> Label_{label.GetHashCode()}");

            _il.Emit(opcode, label);
        }

        /// <summary>
        /// Emits a switch instruction targeting multiple labels (e.g. <c>switch</c>).
        /// Logs all target label IDs as a comma-separated list.
        /// </summary>
        public void Emit(OpCode opcode, Label[] labels)
        {
            if (_enabled)
            {
                var ids = string.Join(",", Array.ConvertAll(labels, l => l.GetHashCode().ToString()));
                _log.AppendLine($"{opcode} -> Labels[{ids}]");
            }
            _il.Emit(opcode, labels);
        }

        // ── Locals ────────────────────────────────────────────────────────────

        /// <summary>
        /// Declares a local variable of the given type and logs its full type name.
        /// The returned <see cref="LocalBuilder"/> is passed to <see cref="Emit(OpCode, LocalBuilder)"/>
        /// overloads to load and store the local in subsequent instructions.
        /// </summary>
        public LocalBuilder DeclareLocal(Type type)
        {
            if (_enabled)
                _log.Append($"Declare local({type.FullName})");
            return _il.DeclareLocal(type);
        }

        // ── Log retrieval ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns the full log of emitted instructions as a newline-delimited string.
        /// Returns an empty string when logging is disabled.
        /// </summary>
        public string GetLog() => _log.ToString();
    }
}