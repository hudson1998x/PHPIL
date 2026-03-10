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
            Console.WriteLine();
            Console.WriteLine("================= EXCEPTION ================");
            Console.WriteLine(ex);
            Console.WriteLine("================= /EXCEPTION ===============");
            Console.WriteLine("================= IL INSPECTOR ================");
            Console.WriteLine(_ilLog?.ToString());
            Console.WriteLine("================= / IL INSPECTOR ================");
        }
    }
}