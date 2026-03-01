using System.Text;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class VariableDeclaration
{
    public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append('{');
        builder.Append("\"type\":\"VariableDeclaration\"");
        builder.Append(",\"name\":");
        VariableName.ToJson(in span, builder);

        if (VariableValue != null)
        {
            builder.Append(",\"expression\": ");
            VariableValue.ToJson(in span, in tokens, builder);
        }

        builder.Append('}');
    }
}