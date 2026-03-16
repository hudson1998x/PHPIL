using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;

namespace PHPIL.Engine.Visitors.SemanticAnalysis;

public partial class SemanticVisitor
{
    public void VisitArrayLiteralNode(ArrayLiteralNode node, in ReadOnlySpan<char> source)
    {
        bool hasStringKeys = false;
        bool hasVariableKeys = false;
        int nextAutoIndex = 0;

        foreach (var item in node.Items)
        {
            item.Value.Accept(this, source);

            if (item.Key != null)
            {
                item.Key.Accept(this, source);

                if (item.Key is LiteralNode keyLiteral)
                {
                    var keyText = keyLiteral.Token.TextValue(in source).Trim('\'');
                    if (!int.TryParse(keyText, out _))
                    {
                        hasStringKeys = true;
                    }
                    else
                    {
                        int keyVal = int.Parse(keyText);
                        if (keyVal >= nextAutoIndex)
                            nextAutoIndex = keyVal + 1;
                    }
                }
                else
                {
                    hasVariableKeys = true;
                }
            }
            else
            {
                if (nextAutoIndex >= 0)
                    nextAutoIndex++;
            }
        }

        node.IsAssociative = hasStringKeys || hasVariableKeys;
        node.AnalysedType = AnalysedType.Array;
    }

    public void VisitSpreadNode(SpreadNode node, in ReadOnlySpan<char> source)
    {
        node.Expression.Accept(this, source);
        node.AnalysedType = node.Expression.AnalysedType;
    }
}