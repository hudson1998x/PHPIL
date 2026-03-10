using System.Text;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class VariableDeclaration
{
    public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append('{');
        builder.Append("\"type\":\"VariableDeclaration\"");
        builder.Append($",\"isUsed\": {IsUsed.ToString().ToLower()}");
        builder.Append(",\"name\":");
        VariableName.ToJson(in span, builder);
        
        builder.Append($",\"isCaptured\": {IsCaptured.ToString().ToLower()}");

        if (VariableValue != null)
        {
            builder.Append($", \"phpType\": \"{VariableValue.AnalysedType}\"");
            builder.Append(",\"value\": ");
            VariableValue.ToJson(in span, in tokens, builder);
        }

        builder.Append('}');
    }
}