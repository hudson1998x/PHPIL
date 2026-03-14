using PHPIL.Engine.Productions.Patterns;

namespace PHPIL.Engine.Productions;

public static partial class Grammar
{
    public static IfExpressionPattern If()
    {
        return new IfExpressionPattern();
    }

    public static SwitchExpressionPattern Switch()
    {
        return new SwitchExpressionPattern();
    }
}