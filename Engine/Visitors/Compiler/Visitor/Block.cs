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

            // Postfix expressions used as statements leave a value on the stack — pop it
            if (stmt is PostfixExpressionNode or PrefixExpressionNode)
                Emit(OpCodes.Pop);
        }
    }
}