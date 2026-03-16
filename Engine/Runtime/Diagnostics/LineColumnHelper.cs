using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.Runtime.Diagnostics;

public static class LineColumnHelper
{
    public static (int, int) GetLineAndColumn(in ReadOnlySpan<char> source, Token token)
    {
        int line = 1;
        int column = 1;

        for (int i = 0; i < token.RangeStart && i < source.Length; i++)
        {
            if (source[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return (line, column);
    }
}