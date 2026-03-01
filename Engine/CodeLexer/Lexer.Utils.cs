namespace PHPIL.Engine.CodeLexer;

public static partial class Lexer
{
     /// <summary>
    /// Seeks the next character
    /// </summary>
    /// <param name="sourceSpan">The source span to search</param>
    /// <param name="start">The start index</param>
    /// <param name="c">The character you're looking for.</param>
    /// <param name="checkEscape">Can this character be escaped?</param>
    /// <returns></returns>
    private static int SeekNext(in ReadOnlySpan<char> sourceSpan, int start, char c, bool checkEscape = true)
    {
        var pointer = start + 1;

        while (pointer < sourceSpan.Length)
        {
            // If current char is a backslash and we're checking escapes,
            // skip the next character entirely (it's escaped)
            if (checkEscape && sourceSpan[pointer] == '\\')
            {
                pointer += 2;
                continue;
            }

            if (sourceSpan[pointer] == c)
            {
                return pointer;
            }

            pointer++;
        }

        return -1; // Not found
    }
    
    /// <summary>
    /// Seeks the next 2 character sequence. 
    /// </summary>
    /// <param name="sourceSpan">The source span to search</param>
    /// <param name="start">The start index</param>
    /// <param name="c">The character you're looking for.</param>
    /// <param name="d">The followup character</param>
    /// <param name="checkEscape">Can this character be escaped?</param>
    /// <returns></returns>
    private static int SeekNext(in ReadOnlySpan<char> sourceSpan, int start, char c, char d, bool checkEscape = true)
    {
        // start from the next character, its a wasted check on the same character
        var pointer = start + 1;
        
        // checks whether 2 characters can be advanced.
        while ((pointer + 1) < sourceSpan.Length)
        {
            // If current char is a backslash and we're checking escapes,
            // skip the next character entirely (it's escaped)
            if (checkEscape && sourceSpan[pointer] == '\\')
            {
                pointer += 2;
                continue;
            }

            if (sourceSpan[pointer] == c && sourceSpan[pointer + 1] == d)
            {
                return pointer;
            }

            pointer++;
        }

        return -1; // Not found
    }

    /// <summary>
    /// Returns the character immediately following the given position,
    /// or null if the position is at or beyond the end of the span.
    /// </summary>
    /// <param name="sourceSpan">The source span to peek into.</param>
    /// <param name="start">The current position. The character at start + 1 is returned.</param>
    /// <returns>The next character, or null if out of bounds.</returns>
    private static char? PeekOne(in ReadOnlySpan<char> sourceSpan, int start)
    {
        if (sourceSpan.Length <= start + 1)
            return null;

        return sourceSpan[start + 1];
    }

    /// <summary>
    /// Checks whether the source span contains the given character sequence starting at the given index.
    /// Used by the main parse loop to identify keywords and multi-character operators.
    /// </summary>
    /// <param name="sourceSpan">The source span to check against.</param>
    /// <param name="start">The index at which to begin matching.</param>
    /// <param name="sequence">The sequence of characters to match.</param>
    /// <returns>True if the span contains the full sequence at the given position, false otherwise.</returns>
    private static bool IsSequence(in ReadOnlySpan<char> sourceSpan, int start, params char[] sequence)
    {
        if (start + sequence.Length > sourceSpan.Length)
            return false;

        for (var i = 0; i < sequence.Length; i++)
        {
            if (sourceSpan[start + i] != sequence[i])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns the end index of the identifier beginning at the given position.
    /// An identifier may contain letters, digits, and underscores.
    /// A leading <c>$</c> (PHP variable sigil) is also consumed if present.
    /// </summary>
    /// <param name="sourceSpan">The source span to scan.</param>
    /// <param name="start">The index of the first character of the identifier.</param>
    /// <returns>
    /// The index of the first character that is not part of the identifier,
    /// or -1 if the identifier runs to the end of the span.
    /// </returns>
    public static int GetIdentifierEnd(in ReadOnlySpan<char> sourceSpan, int start)
    {
        var pointer = start;

        while (pointer < sourceSpan.Length)
        {
            // consume the PHP variable sigil — it is part of the token
            // but not a valid identifier character on its own
            if (sourceSpan[pointer] == '$')
            {
                pointer++;
                continue;
            }

            if (!char.IsLetterOrDigit(sourceSpan[pointer]) && sourceSpan[pointer] != '_')
                return pointer;

            pointer++;
        }

        // identifier ran to EOF
        return -1;
    }

    /// <summary>
    /// Returns the index of the first non-whitespace character after the given position.
    /// Consecutive spaces and tabs are collapsed into a single <see cref="TokenKind.Whitespace"/> token
    /// by advancing past all of them in one call.
    /// </summary>
    /// <param name="sourceSpan">The source span to scan.</param>
    /// <param name="start">The index of the first whitespace character. Scanning begins at start + 1.</param>
    /// <returns>
    /// The index of the first non-whitespace character,
    /// or -1 if the span ends before any non-whitespace character is found.
    /// </returns>
    private static int SeekEndOfWhitespace(in ReadOnlySpan<char> sourceSpan, int start)
    {
        var pointer = start + 1;

        while (pointer < sourceSpan.Length)
        {
            if (!char.IsWhiteSpace(sourceSpan[pointer]))
                return pointer;

            pointer++;
        }

        return -1;
    }
}