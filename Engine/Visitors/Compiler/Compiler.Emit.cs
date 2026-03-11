using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    private readonly DynamicMethod _method;
    private readonly StringBuilder? _ilLog;
    private readonly bool _exposeIl = true;

    public Type ReturnType { get; }
    private Dictionary<string, LocalBuilder> _locals = [];

    public Compiler(string methodName = "phpil_main", Type? returnType = null, Type[]? parameterTypes = null)
    {
        ReturnType = returnType ?? typeof(void);
        _method = new DynamicMethod(methodName, ReturnType, parameterTypes ?? [], typeof(Compiler).Module);

        if (_exposeIl)
        {
            _ilLog = new StringBuilder();
        }
    }

    public DynamicMethod GetDynamicMethod() => _method;

    private ILGenerator GetIl()
    {
        return _method.GetILGenerator();
    }

    private void Emit(OpCode opCode)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::Emit(OpCode opCode) \"{opCode.ToString()}\"");
        GetIl().Emit(opCode);
    }

    private void Emit(OpCode opCode, LocalBuilder nodeLocal)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::Emit(OpCode opCode, LocalBuilder local) \"{opCode.ToString()}\" Index: {nodeLocal.LocalIndex}, Type: {nodeLocal.LocalType.FullName} Is pinned?: {nodeLocal.IsPinned}");
        GetIl().Emit(opCode, nodeLocal);
    }

    private void Emit(OpCode opCode, string textValue)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::Emit(OpCode opCode, string value) \"{opCode.ToString()}\" {textValue}");
        GetIl().Emit(opCode, textValue);
    }

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

    private void Emit(OpCode opCode, double textValue)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::Emit(OpCode opCode, int value) \"{opCode.ToString()}\" {textValue}");
        GetIl().Emit(opCode, textValue);
    }

    private void Emit(OpCode opCode, DynamicMethod nodeLocal)
    {
        GetIl().Emit(opCode, nodeLocal);
    }

    private void Emit(OpCode opCode, Delegate nodeLocal)
    {
        Emit(opCode, nodeLocal.Method);
    }

	private void Emit(OpCode opCode, MethodInfo method)
	{
		if (_exposeIl)
			_ilLog?.AppendLine($"::Emit(OpCode opCode, MethodInfo value) \"{opCode}\" {method.Name}");
		GetIl().Emit(opCode, method);
	}

	private void Emit(OpCode opCode, ConstructorInfo constructor)
	{
		if (_exposeIl)
			_ilLog?.AppendLine($"::Emit(OpCode opCode, ConstructorInfo value) \"{opCode}\" {constructor.DeclaringType?.Name}");
		GetIl().Emit(opCode, constructor);
	}

    private LocalBuilder DeclareLocal(Type type)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::DeclareLocal({type.Name})");
        return GetIl().DeclareLocal(type);
    }

    private void Emit(OpCode opCode, Type nodeLocal)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::Emit(OpCode opCode, Type value) \"{opCode}\" {nodeLocal.Name}");
        GetIl().Emit(opCode, nodeLocal);
    }

    public Label DefineLabel()
    {
        var label = GetIl().DefineLabel();
        if (_exposeIl)
            _ilLog?.AppendLine($"::DefineLabel {label.Id}");
        return label;
    }

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

    private void MarkLabel(Label conditionLabel)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::MarkLabel {conditionLabel.Id}");
        GetIl().MarkLabel(conditionLabel);
    }

    private void Emit(OpCode opCode, Label conditionLabel)
    {
        if (_exposeIl)
            _ilLog?.AppendLine($"::Emit(OpCode opCode, Type value) \"{opCode}\" {conditionLabel.Id}");
        GetIl().Emit(opCode, conditionLabel);
    }
}