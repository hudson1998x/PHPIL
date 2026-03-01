using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace PHPIL.Engine.Visitors.IlProducer
{
    public class ILSpy
    {
        private readonly ILGenerator _il;
        private readonly StringBuilder _log = new();
        private readonly bool _enabled;

        public ILSpy(ILGenerator il, bool enabled = false)
        {
            _il = il ?? throw new ArgumentNullException(nameof(il));
            _enabled = enabled;
        }

        // ---------------- Emit Methods ----------------
        public void Emit(OpCode opcode)
        {
            if (_enabled)
                _log.AppendLine(opcode.ToString());
            _il.Emit(opcode);
        }

        public void Emit(OpCode opcode, int arg)
        {
            if (_enabled)
                _log.AppendLine($"{opcode} {arg}");

            _il.Emit(opcode, arg);
        }
        
        public void Emit(OpCode opcode, double arg)
        {
            if (_enabled)
                _log.AppendLine($"{opcode} {arg}");

            _il.Emit(opcode, arg);
        }

        public void Emit(OpCode opcode, LocalBuilder local)
        {
            if (_enabled)
                _log.AppendLine($"{opcode} local_{local.LocalIndex}");

            _il.Emit(opcode, local);
        }

        public void Emit(OpCode opCode, ConstructorInfo constructor)
        {
            if (_enabled)
                _log.AppendLine($"{opCode} {constructor}");
            _il.Emit(opCode, constructor);
        }
        
        public void Emit(OpCode opCode, FieldInfo fieldInfo)
        {
            if (_enabled)
                _log.AppendLine($"{opCode} {fieldInfo}");
            _il.Emit(opCode, fieldInfo);
        }

        public void Emit(OpCode opcode, MethodInfo method)
        {
            if (_enabled)
                _log.AppendLine($"{opcode} {method.DeclaringType?.Name}.{method.Name}");

            _il.Emit(opcode, method);
        }

        public void Emit(OpCode opcode, Type type)
        {
            if (_enabled)
                _log.AppendLine($"{opcode} {type.Name}");

            _il.Emit(opcode, type);
        }

        public void Emit(OpCode opcode, string str)
        {
            if (_enabled)
                _log.AppendLine($"{opcode} \"{str}\"");

            _il.Emit(opcode, str);
        }

        // ---------------- Label Methods ----------------
        public Label DefineLabel()
        {
            var label = _il.DefineLabel();
            if (_enabled)
                _log.AppendLine($"DefineLabel: {label.GetHashCode()}");
            return label;
        }

        public void MarkLabel(Label label)
        {
            if (_enabled)
                _log.AppendLine($"MarkLabel: {label.GetHashCode()}");

            _il.MarkLabel(label);
        }

        // ---------------- Exception Block / Scope ----------------
        public void BeginScope()
        {
            if (_enabled)
                _log.AppendLine("BeginScope");

            _il.BeginScope();
        }

        public void EndScope()
        {
            if (_enabled)
                _log.AppendLine("EndScope");

            _il.EndScope();
        }

        // ---------------- Branching / jumps ----------------
        public void Emit(OpCode opcode, Label label)
        {
            if (_enabled)
                _log.AppendLine($"{opcode} -> Label_{label.GetHashCode()}");

            _il.Emit(opcode, label);
        }

        public void Emit(OpCode opcode, Label[] labels)
        {
            if (_enabled)
            {
                var ids = string.Join(",", Array.ConvertAll(labels, l => l.GetHashCode().ToString()));
                _log.AppendLine($"{opcode} -> Labels[{ids}]");
            }
            _il.Emit(opcode, labels);
        }

        // ---------------- Get Log ----------------
        public string GetLog() => _log.ToString();

        public LocalBuilder DeclareLocal(Type type)
        {
            if (_enabled)
                _log.Append($"Declare local({type.FullName})");
            return _il.DeclareLocal(type);
        }
    }
}