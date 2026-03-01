using System.Reflection.Emit;
using PHPIL.Engine.Runtime;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors.IlProducer;

public partial class IlProducer : IVisitor
{
    private readonly RuntimeContext _context;
    private DynamicMethod? _runtimeMethod;
    private ILGenerator _ilGenerator;
    private ILSpy _ilSpy;
    public Type? LastEmittedType { get; set; }
    
    public IlProducer()
    {
        _context = new RuntimeContext();
        _runtimeMethod = new DynamicMethod(
            "phpil_main",
            typeof(void),
            []
        );
        _ilGenerator = _runtimeMethod.GetILGenerator();
        _ilSpy = new ILSpy(_ilGenerator, true);
    }

    public IlProducer(ILGenerator ilGenerator)
    {
        _context = new RuntimeContext();
        _ilGenerator = ilGenerator;
        _ilSpy = new ILSpy(_ilGenerator, true);
    }

    public void EnterScope()
    {
        _context.PushFrame(new StackFrame());
    }

    public RuntimeContext GetContext()
    {
        return _context;
    }

    public ILSpy GetILGenerator()
    {
        return _ilSpy;
    }
    
    public void Visit(SyntaxNode node, in ReadOnlySpan<char> span)
    {
        node.Accept(this, in span);
    }

    public void ExitScope()
    {
        _context.PopFrame();
    }

    public void Execute()
    {
        _ilGenerator.Emit(OpCodes.Ret);
        _runtimeMethod?.Invoke(null, null);
    }
}