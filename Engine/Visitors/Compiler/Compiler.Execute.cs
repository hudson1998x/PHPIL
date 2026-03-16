using System.Reflection.Emit;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    
    public void Execute()
    {
        Emit(OpCodes.Ret);
        try
        {
            _method.Invoke(null, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EXCEPTION during execution: {ex}");
            throw;
        }
    }
}