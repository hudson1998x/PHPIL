using System.Reflection.Emit;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    public void VisitBlockNode(BlockNode node, in ReadOnlySpan<char> source)
    {
        foreach (var stmt in node.Statements)
        {
            stmt.Accept(this, in source);

            if (stmt is PostfixExpressionNode or PrefixExpressionNode)
                Emit(OpCodes.Pop);
                
            if (stmt is FunctionCallNode callNode && callNode.Callee is IdentifierNode idNode)
            {
                var func = FunctionTable.GetFunction(idNode.Token.TextValue(in source));
                if (func != null)
                {
                    Type actualReturn = func.Method?.Method.ReturnType ?? func.ReturnType ?? typeof(object);
                    if (actualReturn != typeof(void))
                    {
                        Emit(OpCodes.Pop);
                    }
                }
            }

            if (stmt is BreakNode breakNode)
            {
                return;
            }
        }
    }
}