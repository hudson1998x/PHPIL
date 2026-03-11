using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    public void VisitReturnNode(ReturnNode node, in ReadOnlySpan<char> source)
    {
        if (node.Expression != null)
        {
            node.Expression.Accept(this, source);
            EmitCoercion(node.Expression.AnalysedType, ReturnType);
        }
        
        Emit(OpCodes.Ret);
    }
}
