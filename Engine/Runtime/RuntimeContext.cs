namespace PHPIL.Engine.Runtime;

public class RuntimeContext
{
    // FILO stack of frames, one per function call / scope
    private readonly Stack<StackFrame> _stackFrames = new();

    public StackFrame CurrentFrame => _stackFrames.Peek();

    /// <summary>Push a new frame (function call or scope)</summary>
    public void PushFrame(StackFrame frame)
    {
        _stackFrames.Push(frame);
    }

    /// <summary>Pop the top frame when exiting a scope or function</summary>
    public StackFrame PopFrame()
    {
        return _stackFrames.Pop();
    }

    /// <summary>
    /// Resolve a variable slot by walking up the stack.
    /// Stops at a frame marked as "isolated" (like functions).
    /// </summary>
    public bool TryGetVariableSlot(string name, out int slot)
    {
        slot = -1;

        foreach (var frame in _stackFrames)
        {
            if (frame.TryGetVariableSlot(name, out slot))
                return true;

            if (frame.BreaksTraversal)
                break; // stop at isolated frame
        }

        return false;
    }

    /// <summary>Register a variable in the current frame</summary>
    public int RegisterVariable(string name)
    {
        return _stackFrames.Peek().RegisterVariable(name);
    }
}