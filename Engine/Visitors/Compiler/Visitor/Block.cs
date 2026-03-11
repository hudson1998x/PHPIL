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

            if (stmt is ExpressionNode expr)
            {
                bool shouldPop = true;
                if (expr is BinaryOpNode bin && IsAssignment(bin.Operator) && !bin.NeedsValue)
                {
                    shouldPop = false;
                }
                else if (expr is FunctionCallNode callNode && callNode.Callee is IdentifierNode idNode)
                {
                    var func = FunctionTable.GetFunction(idNode.Token.TextValue(in source));
                    if (func != null)
                    {
                        var method = func.MethodInfo ?? func.Method?.Method;
                        if (method != null && method.ReturnType == typeof(void))
                        {
                            shouldPop = false;
                        }
                        else if (func.ReturnType == typeof(void))
                        {
                            shouldPop = false;
                        }
                    }
                }
                
                if (shouldPop)
                    Emit(OpCodes.Pop);
            }

            if (stmt is BreakNode breakNode)
            {
                return;
            }
        }
    }
}