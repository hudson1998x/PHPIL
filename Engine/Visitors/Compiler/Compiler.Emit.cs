using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// The <see cref="DynamicMethod"/> being compiled, or <see langword="null"/> when this
    /// compiler was constructed from an existing <see cref="ILGenerator"/> (e.g. for class methods).
    /// </summary>
    private readonly DynamicMethod? _method;

    /// <summary>The <see cref="ILGenerator"/> used to emit all IL instructions.</summary>
    private readonly ILGenerator _il;

    /// <summary>
    /// Optional log of all emitted IL instructions and declarations, populated when
    /// <c>_exposeIl</c> is <see langword="true"/>.
    /// </summary>
    private readonly StringBuilder? _ilLog;

    /// <summary>
    /// When <see langword="true"/>, all emit and declare operations are recorded to
    /// <see cref="_ilLog"/> for diagnostic inspection via <see cref="GetILLog"/>.
    /// </summary>
    private readonly bool _exposeIl = true;

    /// <summary>Gets the return type of the method being compiled.</summary>
    public Type ReturnType { get; }

    /// <summary>
    /// Maps PHP variable names to their allocated <see cref="LocalBuilder"/> slots in the
    /// current method scope.
    /// </summary>
    private Dictionary<string, LocalBuilder> _locals = [];

    /// <summary>
    /// Initialises a compiler that emits into a new <see cref="DynamicMethod"/>.
    /// </summary>
    /// <param name="methodName">The name of the dynamic method. Defaults to <c>phpil_main</c>.</param>
    /// <param name="returnType">The return type of the method. Defaults to <see cref="void"/>.</param>
    /// <param name="parameterTypes">The parameter types of the method. Defaults to none.</param>
    public Compiler(string methodName = "phpil_main", Type? returnType = null, Type[]? parameterTypes = null)
    {
        ReturnType = returnType ?? typeof(void);
        _method = new DynamicMethod(methodName, ReturnType, parameterTypes ?? [], typeof(Compiler).Module);
        _il = _method.GetILGenerator();

        if (_exposeIl)
        {
            _ilLog = new StringBuilder();
        }
    }

    /// <summary>
    /// Initialises a compiler that emits into an existing <see cref="ILGenerator"/>, used
    /// when compiling class method bodies inside a <see cref="System.Reflection.Emit.MethodBuilder"/>.
    /// </summary>
    /// <param name="il">The <see cref="ILGenerator"/> to emit into.</param>
    /// <param name="returnType">The return type of the method being compiled.</param>
    public Compiler(ILGenerator il, Type returnType)
    {
        _il = il;
        ReturnType = returnType;
        if (_exposeIl)
        {
            _ilLog = new StringBuilder();
        }
    }

    /// <summary>
    /// Returns the underlying <see cref="DynamicMethod"/>, or <see langword="null"/> if this
    /// compiler was constructed from an existing <see cref="ILGenerator"/>.
    /// </summary>
    public DynamicMethod? GetDynamicMethod() => _method;

    /// <summary>
    /// Returns the accumulated IL log as a string, or an empty string if logging is disabled
    /// or no instructions have been emitted.
    /// </summary>
    public string GetILLog() => _ilLog?.ToString() ?? "";

    /// <summary>Returns the active <see cref="ILGenerator"/>.</summary>
    private ILGenerator GetIl() => _il;

    /// <summary>Emits a single opcode with no operand.</summary>
    private void Emit(OpCode opCode)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::Emit(OpCode opCode) \"{opCode.ToString()}\"");
        GetIl().Emit(opCode);
    }

    /// <summary>Emits an opcode with a <see cref="LocalBuilder"/> operand.</summary>
    private void Emit(OpCode opCode, LocalBuilder nodeLocal)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::Emit(OpCode opCode, LocalBuilder local) \"{opCode.ToString()}\" Index: {nodeLocal.LocalIndex}, Type: {nodeLocal.LocalType.FullName} Is pinned?: {nodeLocal.IsPinned}");
        GetIl().Emit(opCode, nodeLocal);
    }

    /// <summary>Emits an opcode with a <see cref="string"/> operand.</summary>
    private void Emit(OpCode opCode, string textValue)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::Emit(OpCode opCode, string value) \"{opCode.ToString()}\" {textValue}");
        GetIl().Emit(opCode, textValue);
    }

    /// <summary>
    /// Emits an opcode with an <see cref="int"/> operand, automatically substituting the
    /// optimal short-form <c>Ldc_I4_*</c> opcode for values in the range <c>-1</c> to <c>8</c>.
    /// </summary>
    private void Emit(OpCode opCode, int textValue)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::Emit(OpCode opCode, int value) \"{opCode.ToString()}\" {textValue}");
        switch (textValue)
        {
            case -1: GetIl().Emit(OpCodes.Ldc_I4_M1); break;
            case 0:  GetIl().Emit(OpCodes.Ldc_I4_0);  break;
            case 1:  GetIl().Emit(OpCodes.Ldc_I4_1);  break;
            case 2:  GetIl().Emit(OpCodes.Ldc_I4_2);  break;
            case 3:  GetIl().Emit(OpCodes.Ldc_I4_3);  break;
            case 4:  GetIl().Emit(OpCodes.Ldc_I4_4);  break;
            case 5:  GetIl().Emit(OpCodes.Ldc_I4_5);  break;
            case 6:  GetIl().Emit(OpCodes.Ldc_I4_6);  break;
            case 7:  GetIl().Emit(OpCodes.Ldc_I4_7);  break;
            case 8:  GetIl().Emit(OpCodes.Ldc_I4_8);  break;
            default: GetIl().Emit(OpCodes.Ldc_I4, textValue); break;
        }
    }

    /// <summary>Emits an opcode with a <see cref="double"/> operand.</summary>
    private void Emit(OpCode opCode, double textValue)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::Emit(OpCode opCode, int value) \"{opCode.ToString()}\" {textValue}");
        GetIl().Emit(opCode, textValue);
    }

    /// <summary>Emits an opcode with a <see cref="DynamicMethod"/> operand.</summary>
    private void Emit(OpCode opCode, DynamicMethod nodeLocal)
    {
        GetIl().Emit(opCode, nodeLocal);
    }

    /// <summary>
    /// Emits an opcode targeting the underlying <see cref="MethodInfo"/> of a delegate.
    /// </summary>
    private void Emit(OpCode opCode, Delegate nodeLocal)
    {
        Emit(opCode, nodeLocal.Method);
    }

    /// <summary>Emits an opcode with a <see cref="MethodInfo"/> operand.</summary>
    private void Emit(OpCode opCode, MethodInfo method)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::Emit(OpCode opCode, MethodInfo value) \"{opCode}\" {method.Name}");
        GetIl().Emit(opCode, method);
    }

    /// <summary>Emits an opcode with a <see cref="ConstructorInfo"/> operand.</summary>
    private void Emit(OpCode opCode, ConstructorInfo constructor)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::Emit(OpCode opCode, ConstructorInfo value) \"{opCode}\" {constructor.DeclaringType?.Name}");
        GetIl().Emit(opCode, constructor);
    }

    /// <summary>Emits an opcode with a <see cref="FieldInfo"/> operand.</summary>
    private void Emit(OpCode opCode, FieldInfo field)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::Emit(OpCode opCode, FieldInfo value) \"{opCode}\" {field.Name}");
        GetIl().Emit(opCode, field);
    }

    /// <summary>
    /// Declares a new local variable of the given type in the current method scope,
    /// logging the declaration when IL logging is enabled.
    /// </summary>
    /// <param name="type">The CLR type of the local variable to declare.</param>
    /// <returns>The <see cref="LocalBuilder"/> representing the allocated local slot.</returns>
    private LocalBuilder DeclareLocal(Type type)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::DeclareLocal({type.Name})");
        return GetIl().DeclareLocal(type);
    }

    /// <summary>Emits an opcode with a <see cref="Type"/> operand.</summary>
    private void Emit(OpCode opCode, Type nodeLocal)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::Emit(OpCode opCode, Type value) \"{opCode}\" {nodeLocal.Name}");
        GetIl().Emit(opCode, nodeLocal);
    }

    /// <summary>
    /// Defines a new <see cref="Label"/> in the current method body, logging its ID when
    /// IL logging is enabled.
    /// </summary>
    /// <returns>The newly defined <see cref="Label"/>.</returns>
    public Label DefineLabel()
    {
        var label = GetIl().DefineLabel();
        if (_exposeIl)
            _ilLog?.AppendLine($"::DefineLabel {label.Id}");
        return label;
    }

    /// <summary>
    /// Emits IL to coerce a value from a given <see cref="AnalysedType"/> to a target CLR type.
    /// </summary>
    /// <param name="from">The <see cref="AnalysedType"/> of the value currently on the stack.</param>
    /// <param name="to">The CLR type to coerce the value to.</param>
    /// <exception cref="NotImplementedException">
    /// Thrown when the requested coercion is not yet supported.
    /// </exception>
    /// <remarks>
    /// Supported coercions: to <see cref="string"/> via <see cref="EmitStringCoercion"/>; to
    /// <see cref="object"/> by boxing <see cref="int"/>, <see cref="double"/>, or <see cref="bool"/>
    /// as needed; to <see cref="int"/> as a no-op; to <see cref="double"/> by emitting
    /// <see cref="OpCodes.Conv_R8"/> when the source is <see cref="AnalysedType.Int"/>; and to
    /// <see cref="bool"/> as a no-op.
    /// </remarks>
    private void EmitCoercion(AnalysedType from, Type to)
    {
        if (to == typeof(string))
        {
            EmitStringCoercion(from);
            return;
        }

        if (to == typeof(object))
        {
            if (from == AnalysedType.Int) Emit(OpCodes.Box, typeof(int));
            if (from == AnalysedType.Float) Emit(OpCodes.Box, typeof(double));
            if (from == AnalysedType.Boolean) Emit(OpCodes.Box, typeof(bool));
            return;
        }

        if (to == typeof(int))
            return;

        if (to == typeof(double))
        {
            if (from == AnalysedType.Int)
                Emit(OpCodes.Conv_R8);
            return;
        }

        if (to == typeof(bool))
            return;

        throw new NotImplementedException($"Cannot coerce {from} to {to.Name} yet.");
    }

    /// <summary>
    /// Emits IL to coerce a value on the stack to its unboxed numeric form, if necessary.
    /// </summary>
    /// <param name="type">The <see cref="AnalysedType"/> of the value on the stack.</param>
    /// <exception cref="NotImplementedException">
    /// Thrown when the type cannot be coerced to a numeric value.
    /// </exception>
    /// <remarks>
    /// <see cref="AnalysedType.Mixed"/> values are unboxed to <see cref="int"/> via
    /// <see cref="OpCodes.Unbox_Any"/>. <see cref="AnalysedType.Int"/> and
    /// <see cref="AnalysedType.Float"/> are already in numeric form and require no instruction.
    /// </remarks>
    private void EmitNumericCoercion(AnalysedType type)
    {
        switch (type)
        {
            case AnalysedType.Mixed:
                Emit(OpCodes.Unbox_Any, typeof(int));
                break;
            case AnalysedType.Int:
            case AnalysedType.Float:
                break;
            default:
                throw new NotImplementedException($"Cannot coerce {type} to numeric yet.");
        }
    }

    /// <summary>
    /// Marks a previously defined <see cref="Label"/> at the current position in the IL stream,
    /// logging its ID when IL logging is enabled.
    /// </summary>
    /// <param name="conditionLabel">The <see cref="Label"/> to mark.</param>
    private void MarkLabel(Label conditionLabel)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::MarkLabel {conditionLabel.Id}");
        GetIl().MarkLabel(conditionLabel);
    }

    /// <summary>Emits an opcode with a <see cref="Label"/> operand.</summary>
    private void Emit(OpCode opCode, Label conditionLabel)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::Emit(OpCode opCode, Type value) \"{opCode}\" {conditionLabel.Id}");
        GetIl().Emit(opCode, conditionLabel);
    }
}