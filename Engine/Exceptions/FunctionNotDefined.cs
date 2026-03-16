namespace PHPIL.Engine.Exceptions;

public class FunctionNotDefinedException : Exception
{
    public FunctionNotDefinedException(string functionName, string fileName, int line = 0, int column = 0) : base(
        $"Unknown function '{functionName}' in file {fileName} Line {line}, {column}")
    {
        
    }
}