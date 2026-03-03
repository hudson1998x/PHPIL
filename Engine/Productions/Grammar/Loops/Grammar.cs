using PHPIL.Engine.Productions.Patterns;

namespace PHPIL.Engine.Productions;

public static partial class Grammar
{
    public static WhileExpressionPattern WhileExpression()
    {
        return new WhileExpressionPattern();
    }
}