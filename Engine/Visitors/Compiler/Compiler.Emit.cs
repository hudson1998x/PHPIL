using System.Reflection.Emit;
using System.Text;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{

    private readonly DynamicMethod _method;
    
    private readonly StringBuilder? _ilLog;

    private readonly bool _exposeIl = true;
    
    /// <summary>
    /// We can use a simple $var, LocalBuilder dictionary here
    /// since every function will have its own dynamic method builder,
    /// with its own context etc... A script will just be a method
    /// </summary>
    private Dictionary<string, LocalBuilder> _locals = [];
    
    public Compiler(string methodName = "phpil_main")
    {
        _method = new DynamicMethod(methodName, typeof(void), []);

        if (_exposeIl)
        {
            _ilLog = new StringBuilder();
        }
    }

    private ILGenerator GetIl()
    {
        return _method.GetILGenerator();
    }


    private void Emit(OpCode opCode)
    {
        if (_exposeIl)
        {
            _ilLog?.AppendLine($"::Emit(OpCode opCode) \"{opCode.ToString()}\"");
        }
        GetIl().Emit(opCode);
    }

    private void Emit(OpCode opCode, LocalBuilder nodeLocal)
    {
        if (_exposeIl)
        {
            _ilLog?.AppendLine($"::Emit(OpCode opCode, LocalBuilder local) \"{opCode.ToString()}\" Index: {nodeLocal.LocalIndex}, Type: {nodeLocal.LocalType.FullName} Is pinned?: {nodeLocal.IsPinned}");
        }
        GetIl().Emit(opCode, nodeLocal);
    }

    private void Emit(OpCode opCode, string textValue)
    {
        if (_exposeIl)
        {
            _ilLog?.AppendLine($"::Emit(OpCode opCode, string value) \"{opCode.ToString()}\" {textValue}");
        }
        GetIl().Emit(opCode, textValue);
    }
    
    private void Emit(OpCode opCode, int textValue)
    {
        if (_exposeIl)
        {
            _ilLog?.AppendLine($"::Emit(OpCode opCode, int value) \"{opCode.ToString()}\" {textValue}");
        }
        switch (textValue)
        {
            case -1: GetIl().Emit(OpCodes.Ldc_I4_M1); break;
            case 0: GetIl().Emit(OpCodes.Ldc_I4_0); break;
            case 1: GetIl().Emit(OpCodes.Ldc_I4_1); break;
            case 2: GetIl().Emit(OpCodes.Ldc_I4_2); break;
            case 3: GetIl().Emit(OpCodes.Ldc_I4_3); break;
            case 4: GetIl().Emit(OpCodes.Ldc_I4_4); break;
            case 5: GetIl().Emit(OpCodes.Ldc_I4_5); break;
            case 6: GetIl().Emit(OpCodes.Ldc_I4_6); break;
            case 7: GetIl().Emit(OpCodes.Ldc_I4_7); break;
            case 8: GetIl().Emit(OpCodes.Ldc_I4_8); break;
            default: GetIl().Emit(OpCodes.Ldc_I4, textValue); break;
        }
    }
    
    private void Emit(OpCode opCode, double textValue)
    {
        if (_exposeIl)
        {
            _ilLog?.AppendLine($"::Emit(OpCode opCode, int value) \"{opCode.ToString()}\" {textValue}");
        }
        GetIl().Emit(opCode, textValue);
    }

    private LocalBuilder DeclareLocal(Type type)
    {
        if (_exposeIl)
        {
            _ilLog?.AppendLine($"::DeclareLocal({type.Name})");
        }

        return GetIl().DeclareLocal(type);
    }
}