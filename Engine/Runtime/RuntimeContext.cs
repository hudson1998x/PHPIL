using System.Collections.Generic;
using System.Reflection.Emit;

namespace PHPIL.Engine.Runtime;

public class RuntimeContext
{
    private readonly Stack<StackFrame> _stackFrames = new();

    public StackFrame CurrentFrame => _stackFrames.Peek();

    public void PushFrame(StackFrame frame) => _stackFrames.Push(frame);

    public StackFrame PopFrame() => _stackFrames.Pop();

    public bool TryGetVariableSlot(string name, out int slot)
    {
        slot = -1;
        foreach (var frame in _stackFrames)
        {
            if (frame.TryGetVariableSlot(name, out slot))
                return true;

            if (frame.BreaksTraversal)
                break;
        }
        return false;
    }

    // FIX: Pass the ILGenerator through so the frame can declare the local
    public int RegisterVariable(string name, ILGenerator il)
    {
        return _stackFrames.Peek().RegisterVariable(name, il);
    }
}