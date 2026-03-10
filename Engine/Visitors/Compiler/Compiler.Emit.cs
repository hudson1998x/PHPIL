using System.Reflection.Emit;
using System.Text;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{

    private readonly DynamicMethod _method;
    
    private readonly StringBuilder? _ilLog;

    private readonly bool _exposeIl = true;
    
    public Compiler()
    {
        _method = new DynamicMethod("phpil_main", typeof(void), []);

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
        GetIl().Emit(opCode, textValue);
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