namespace PHPIL.Engine.CodeLexer;

public static partial class Lexer
{
    public static Token[] ParseFile(string filePath)
    {
        return ParseSpan(File.ReadAllText(filePath).AsSpan());
    }

    public static Token[] ParseSpan(in ReadOnlySpan<char> sourceSpan, bool isTestSuite = false)
    {
        // a fair assumption is made here, there will never be
        // more tokens than half, considering legal strings are a minimum of 2 chars,
        // variables are a minimum of 2 chars, keywords >= 2 chars
        var tokens = new Token[isTestSuite ? sourceSpan.Length : sourceSpan.Length / 2];
        var tokenIndex = 0;

        // hold the current pointer, this should ALWAYS increment, at least by 1
        // but can of course increment by more than that.
        var position = 0;
        
        // now for the main parse loop.
        while (position < sourceSpan.Length)
        {
            char c = sourceSpan[position];

            switch (c)
            {
                // =====================
                // STRING LITERALS
                // =====================
                
                case '\'':
                case '"':
                    var end = SeekNext(in sourceSpan, position, c);
                    if (end == -1)
                    {
                        MarkUnknown();
                        continue;
                    }
                    AddToken(TokenKind.StringLiteral, end + 1);
                    continue;
                
                // =====================
                // COMMENTS + DIVISION
                // =====================
                
                case '/':
                    var next = PeekOne(in sourceSpan, position);
                    if (next is null)
                    {
                        MarkUnknown();
                        continue;
                    }
                    if (next is '/')
                    {
                        var nextEol = SeekNext(in sourceSpan, position, '\n', checkEscape: false);
                        AddToken(TokenKind.SingleLineComment, nextEol == -1 ? sourceSpan.Length : nextEol);
                        continue;
                    }
                    if (next is '*')
                    {
                        var endIndex = SeekNext(in sourceSpan, position, '*', '/');
                        if (endIndex == -1)
                        {
                            MarkUnknown();
                            continue;
                        }
                        AddToken(TokenKind.MultiLineComment, endIndex + 2);
                        continue;
                    }
                    if (next is '=')
                    {
                        AddToken(TokenKind.DivideAssign, position + 2);
                        continue;
                    }
                    AddToken(TokenKind.DivideBy, position + 1);
                    break;
                
                // =====================
                // VARIABLES
                // =====================
                
                case '$':
                    var varEnd = GetIdentifierEnd(in sourceSpan, position);
                    if (varEnd == -1)
                    {
                        MarkUnknown();
                        continue;
                    }
                    AddToken(TokenKind.Variable, varEnd);
                    break;
                
                // =====================
                // NUMERIC LITERALS
                // =====================
                
                case '0': case '1': case '2': case '3': case '4':
                case '5': case '6': case '7': case '8': case '9':
                    var numEnd = SeekEndOfNumber(in sourceSpan, position);
                    // peek whether this ended on a '.' to decide int vs float
                    var isFloat = numEnd > position 
                                  && numEnd <= sourceSpan.Length 
                                  && sourceSpan[position..numEnd].Contains('.');
                    AddToken(isFloat ? TokenKind.FloatLiteral : TokenKind.IntLiteral, numEnd);
                    break;
                
                // =====================
                // PUNCTUATION
                // =====================
                
                case ';':
                    AddToken(TokenKind.ExpressionTerminator, position + 1);
                    break;
                
                case ',':
                    AddToken(TokenKind.Comma, position + 1);
                    break;
                
                case '@':
                    AddToken(TokenKind.ErrorSuppress, position + 1);
                    break;
                
                case '\\':
                    AddToken(TokenKind.NamespaceSeparator, position + 1);
                    break;
                
                case '`':
                    var shellEnd = SeekNext(in sourceSpan, position, '`');
                    if (shellEnd == -1) { MarkUnknown(); continue; }
                    AddToken(TokenKind.ShellExecute, shellEnd + 1);
                    continue;
                
                case ':':
                    if (IsSequence(in sourceSpan, position, ':', ':'))
                    {
                        AddToken(TokenKind.ScopeResolution, position + 2);
                        continue;
                    }
                    AddToken(TokenKind.Colon, position + 1);
                    break;
                
                // =====================
                // WHITESPACE + NEWLINES
                // =====================
                
                case ' ':
                case '\t':
                    var endOfWhitespace = SeekEndOfWhitespace(in sourceSpan, position);
                    AddToken(TokenKind.Whitespace, endOfWhitespace < 0 ? sourceSpan.Length : endOfWhitespace);
                    break;
                
                case '\r':
                    position++;
                    continue;
                
                case '\n':
                    AddToken(TokenKind.NewLine, position + 1);
                    break;
                
                // =====================
                // BRACKETS / PARENS / BRACES
                // =====================
                
                case '(':
                    AddToken(TokenKind.LeftParen, position + 1);
                    break;
                case ')':
                    AddToken(TokenKind.RightParen, position + 1);
                    break;
                case '{':
                    AddToken(TokenKind.LeftBrace, position + 1);
                    break;
                case '}':
                    AddToken(TokenKind.RightBrace, position + 1);
                    break;
                case '[':
                    AddToken(TokenKind.LeftBracket, position + 1);
                    break;
                case ']':
                    AddToken(TokenKind.RightBracket, position + 1);
                    break;
                
                // =====================
                // ANGLE BRACKETS, TAGS, SHIFTS
                // =====================
                
                case '<':
                    if (IsSequence(in sourceSpan, position, '<', '?', 'p', 'h', 'p'))
                    {
                        AddToken(TokenKind.PhpOpenTag, position + 5);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '<', '=', '>'))
                    {
                        AddToken(TokenKind.Spaceship, position + 3);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '<', '<', '='))
                    {
                        AddToken(TokenKind.LeftShiftAssign, position + 3);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '<', '<'))
                    {
                        AddToken(TokenKind.LeftShift, position + 2);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '<', '='))
                    {
                        AddToken(TokenKind.LessThanOrEqual, position + 2);
                        continue;
                    }
                    AddToken(TokenKind.LessThan, position + 1);
                    break;
                
                case '>':
                    if (IsSequence(in sourceSpan, position, '>', '>', '='))
                    {
                        AddToken(TokenKind.RightShiftAssign, position + 3);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '>', '>'))
                    {
                        AddToken(TokenKind.RightShift, position + 2);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '>', '='))
                    {
                        AddToken(TokenKind.GreaterThanOrEqual, position + 2);
                        continue;
                    }
                    AddToken(TokenKind.GreaterThan, position + 1);
                    break;
                
                // =====================
                // STRING CONCAT
                // =====================
                
                case '.':
                    if (IsSequence(in sourceSpan, position, '.', '.', '.'))
                    {
                        AddToken(TokenKind.CollectSpread, position + 3);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '.', '='))
                    {
                        AddToken(TokenKind.ConcatAppend, position + 2);
                        continue;
                    }
                    AddToken(TokenKind.Concat, position + 1);
                    continue;
                
                // =====================
                // EQUALITY + ASSIGNMENT
                // =====================
                
                case '=':
                    if (IsSequence(in sourceSpan, position, '=', '=', '='))
                    {
                        AddToken(TokenKind.DeepEquality, position + 3);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '=', '>'))
                    {
                        AddToken(TokenKind.Arrow, position + 2);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '=', '='))
                    {
                        AddToken(TokenKind.ShallowEquality, position + 2);
                        continue;
                    }
                    AddToken(TokenKind.AssignEquals, position + 1);
                    break;
                
                // =====================
                // INEQUALITY + NOT
                // =====================
                
                case '!':
                    if (IsSequence(in sourceSpan, position, '!', '=', '='))
                    {
                        AddToken(TokenKind.DeepInequality, position + 3);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '!', '='))
                    {
                        AddToken(TokenKind.ShallowInequality, position + 2);
                        continue;
                    }
                    AddToken(TokenKind.Not, position + 1);
                    break;
                
                // =====================
                // BITWISE + LOGICAL AND
                // =====================
                
                case '&':
                    if (IsSequence(in sourceSpan, position, '&', '&'))
                    {
                        AddToken(TokenKind.LogicalAnd, position + 2);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '&', '='))
                    {
                        AddToken(TokenKind.BitwiseAndAssign, position + 2);
                        continue;
                    }
                    AddToken(TokenKind.Ampersand, position + 1);
                    break;
                
                // =====================
                // BITWISE + LOGICAL OR
                // =====================
                
                case '|':
                    if (IsSequence(in sourceSpan, position, '|', '|'))
                    {
                        AddToken(TokenKind.LogicalOr, position + 2);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '|', '='))
                    {
                        AddToken(TokenKind.BitwiseOrAssign, position + 2);
                        continue;
                    }
                    AddToken(TokenKind.BitwiseOr, position + 1);
                    break;
                
                // =====================
                // XOR
                // =====================
                
                case '^':
                    if (IsSequence(in sourceSpan, position, '^', '='))
                    {
                        AddToken(TokenKind.BitwiseXorAssign, position + 2);
                        continue;
                    }
                    AddToken(TokenKind.BitwiseXor, position + 1);
                    break;
                
                // =====================
                // BITWISE NOT
                // =====================
                
                case '~':
                    AddToken(TokenKind.BitwiseNot, position + 1);
                    break;
                
                // =====================
                // ARITHMETIC
                // =====================
                
                case '+':
                    if (IsSequence(in sourceSpan, position, '+', '+'))
                    {
                        AddToken(TokenKind.Increment, position + 2);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '+', '='))
                    {
                        AddToken(TokenKind.AddAssign, position + 2);
                        continue;
                    }
                    AddToken(TokenKind.Add, position + 1);
                    break;
                
                case '-':
                    if (IsSequence(in sourceSpan, position, '-', '-'))
                    {
                        AddToken(TokenKind.Decrement, position + 2);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '-', '='))
                    {
                        AddToken(TokenKind.SubtractAssign, position + 2);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '-', '>'))
                    {
                        AddToken(TokenKind.ObjectOperator, position + 2);
                        continue;
                    }
                    AddToken(TokenKind.Subtract, position + 1);
                    break;
                
                case '*':
                    if (IsSequence(in sourceSpan, position, '*', '*', '='))
                    {
                        AddToken(TokenKind.PowerAssign, position + 3);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '*', '*'))
                    {
                        AddToken(TokenKind.Power, position + 2);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '*', '='))
                    {
                        AddToken(TokenKind.MultiplyAssign, position + 2);
                        continue;
                    }
                    AddToken(TokenKind.Multiply, position + 1);
                    break;
                
                case '%':
                    if (IsSequence(in sourceSpan, position, '%', '='))
                    {
                        AddToken(TokenKind.ModuloAssign, position + 2);
                        continue;
                    }
                    AddToken(TokenKind.Modulo, position + 1);
                    break;
                
                // =====================
                // NULL COALESCING + TERNARY
                // =====================
                
                case '?':
                    if (IsSequence(in sourceSpan, position, '?', '>'))
                    {
                        AddToken(TokenKind.PhpCloseTag, position + 2);
                    }
                    if (IsSequence(in sourceSpan, position, '?', '?', '='))
                    {
                        AddToken(TokenKind.NullCoalesceAssign, position + 3);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '?', '?'))
                    {
                        AddToken(TokenKind.NullCoalesce, position + 2);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, '?', '-', '>'))
                    {
                        AddToken(TokenKind.NullsafeOperator, position + 3);
                        continue;
                    }
                    AddToken(TokenKind.QuestionMark, position + 1);
                    break;
                
                // =====================
                // HASH COMMENTS
                // =====================
                
                case '#':
                    var hashEol = SeekNext(in sourceSpan, position, '\n', checkEscape: false);
                    AddToken(TokenKind.SingleLineComment, hashEol == -1 ? sourceSpan.Length : hashEol);
                    continue;
                
                // =====================
                // KEYWORDS
                // =====================
                
                // Note: within each letter, always check longer sequences first
                // to avoid a short keyword matching the prefix of a longer one.
                // Every match is guarded by IsKeywordBoundary to prevent
                // e.g. 'if' matching the start of 'ifdef'.
                // If no keyword matches, goto default to fall through to identifier.
                
                case 'a':
                    if (IsSequence(in sourceSpan, position, 'a', 'b', 's', 't', 'r', 'a', 'c', 't')
                        && IsKeywordBoundary(in sourceSpan, position + 8))
                    {
                        AddToken(TokenKind.Abstract, position + 8);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'a', 'r', 'r', 'a', 'y')
                        && IsKeywordBoundary(in sourceSpan, position + 5))
                    {
                        AddToken(TokenKind.Array, position + 5);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'a', 'n', 'd')
                        && IsKeywordBoundary(in sourceSpan, position + 3))
                    {
                        AddToken(TokenKind.LogicalAndKeyword, position + 3);
                        continue;
                    }
                    goto default;
                
                case 'b':
                    if (IsSequence(in sourceSpan, position, 'b', 'r', 'e', 'a', 'k')
                        && IsKeywordBoundary(in sourceSpan, position + 5))
                    {
                        AddToken(TokenKind.Break, position + 5);
                        continue;
                    }
                    goto default;
                
                case 'c':
                    if (IsSequence(in sourceSpan, position, 'c', 'o', 'n', 't', 'i', 'n', 'u', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 8))
                    {
                        AddToken(TokenKind.Continue, position + 8);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'c', 'a', 'l', 'l', 'a', 'b', 'l', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 8))
                    {
                        AddToken(TokenKind.Callable, position + 8);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'c', 'a', 't', 'c', 'h')
                        && IsKeywordBoundary(in sourceSpan, position + 5))
                    {
                        AddToken(TokenKind.Catch, position + 5);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'c', 'l', 'a', 's', 's')
                        && IsKeywordBoundary(in sourceSpan, position + 5))
                    {
                        AddToken(TokenKind.Class, position + 5);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'c', 'o', 'n', 's', 't')
                        && IsKeywordBoundary(in sourceSpan, position + 5))
                    {
                        AddToken(TokenKind.Const, position + 5);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'c', 'a', 's', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 4))
                    {
                        AddToken(TokenKind.Case, position + 4);
                        continue;
                    }
                    goto default;
                
                case 'd':
                    if (IsSequence(in sourceSpan, position, 'd', 'e', 'c', 'l', 'a', 'r', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 7))
                    {
                        AddToken(TokenKind.Declare, position + 7);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'd', 'e', 'f', 'a', 'u', 'l', 't')
                        && IsKeywordBoundary(in sourceSpan, position + 7))
                    {
                        AddToken(TokenKind.Default, position + 7);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'd', 'i', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 3))
                    {
                        AddToken(TokenKind.Die, position + 3);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'd', 'o')
                        && IsKeywordBoundary(in sourceSpan, position + 2))
                    {
                        AddToken(TokenKind.Do, position + 2);
                        continue;
                    }
                    goto default;
                
                case 'e':
                    if (IsSequence(in sourceSpan, position, 'e', 'n', 'd', 'd', 'e', 'c', 'l', 'a', 'r', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 10))
                    {
                        AddToken(TokenKind.EndDeclare, position + 10);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'e', 'n', 'd', 'f', 'o', 'r', 'e', 'a', 'c', 'h')
                        && IsKeywordBoundary(in sourceSpan, position + 10))
                    {
                        AddToken(TokenKind.EndForeach, position + 10);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'e', 'n', 'd', 's', 'w', 'i', 't', 'c', 'h')
                        && IsKeywordBoundary(in sourceSpan, position + 9))
                    {
                        AddToken(TokenKind.EndSwitch, position + 9);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'e', 'n', 'd', 'w', 'h', 'i', 'l', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 8))
                    {
                        AddToken(TokenKind.EndWhile, position + 8);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'e', 'n', 'd', 'f', 'o', 'r')
                        && IsKeywordBoundary(in sourceSpan, position + 6))
                    {
                        AddToken(TokenKind.EndFor, position + 6);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'e', 'n', 'd', 'i', 'f')
                        && IsKeywordBoundary(in sourceSpan, position + 5))
                    {
                        AddToken(TokenKind.EndIf, position + 5);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'e', 'x', 't', 'e', 'n', 'd', 's')
                        && IsKeywordBoundary(in sourceSpan, position + 7))
                    {
                        AddToken(TokenKind.Extends, position + 7);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'e', 'l', 's', 'e', 'i', 'f')
                        && IsKeywordBoundary(in sourceSpan, position + 6))
                    {
                        AddToken(TokenKind.ElseIf, position + 6);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'e', 'l', 's', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 4))
                    {
                        AddToken(TokenKind.Else, position + 4);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'e', 'c', 'h', 'o')
                        && IsKeywordBoundary(in sourceSpan, position + 4))
                    {
                        AddToken(TokenKind.Identifier, position + 4);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'e', 'n', 'u', 'm')
                        && IsKeywordBoundary(in sourceSpan, position + 4))
                    {
                        AddToken(TokenKind.Enum, position + 4);
                        continue;
                    }
                    goto default;
                
                case 'f':
                    if (IsSequence(in sourceSpan, position, 'f', 'o', 'r', 'e', 'a', 'c', 'h')
                        && IsKeywordBoundary(in sourceSpan, position + 7))
                    {
                        AddToken(TokenKind.Foreach, position + 7);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'f', 'u', 'n', 'c', 't', 'i', 'o', 'n')
                        && IsKeywordBoundary(in sourceSpan, position + 8))
                    {
                        AddToken(TokenKind.Function, position + 8);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'f', 'i', 'n', 'a', 'l', 'l', 'y')
                        && IsKeywordBoundary(in sourceSpan, position + 7))
                    {
                        AddToken(TokenKind.Finally, position + 7);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'f', 'a', 'l', 's', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 5))
                    {
                        AddToken(TokenKind.FalseLiteral, position + 5);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'f', 'i', 'n', 'a', 'l')
                        && IsKeywordBoundary(in sourceSpan, position + 5))
                    {
                        AddToken(TokenKind.Final, position + 5);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'f', 'o', 'r')
                        && IsKeywordBoundary(in sourceSpan, position + 3))
                    {
                        AddToken(TokenKind.For, position + 3);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'f', 'n')
                        && IsKeywordBoundary(in sourceSpan, position + 2))
                    {
                        AddToken(TokenKind.Fn, position + 2);
                        continue;
                    }
                    goto default;
                
                case 'g':
                    if (IsSequence(in sourceSpan, position, 'g', 'o', 't', 'o')
                        && IsKeywordBoundary(in sourceSpan, position + 4))
                    {
                        AddToken(TokenKind.Goto, position + 4);
                        continue;
                    }
                    goto default;
                
                case 'i':
                    if (IsSequence(in sourceSpan, position, 'i', 'm', 'p', 'l', 'e', 'm', 'e', 'n', 't', 's')
                        && IsKeywordBoundary(in sourceSpan, position + 10))
                    {
                        AddToken(TokenKind.Implements, position + 10);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'i', 'n', 's', 't', 'a', 'n', 'c', 'e', 'o', 'f')
                        && IsKeywordBoundary(in sourceSpan, position + 10))
                    {
                        AddToken(TokenKind.Instanceof, position + 10);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'i', 'n', 't', 'e', 'r', 'f', 'a', 'c', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 9))
                    {
                        AddToken(TokenKind.Interface, position + 9);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'i', 'n', 's', 't', 'e', 'a', 'd', 'o', 'f')
                        && IsKeywordBoundary(in sourceSpan, position + 9))
                    {
                        AddToken(TokenKind.Insteadof, position + 9);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'i', 'n', 'c', 'l', 'u', 'd', 'e', '_', 'o', 'n', 'c', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 12))
                    {
                        AddToken(TokenKind.Identifier, position + 12);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'i', 'n', 'c', 'l', 'u', 'd', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 7))
                    {
                        AddToken(TokenKind.Identifier, position + 7);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'i', 'f')
                        && IsKeywordBoundary(in sourceSpan, position + 2))
                    {
                        AddToken(TokenKind.If, position + 2);
                        continue;
                    }
                    goto default;
                
                case 'l':
                    if (IsSequence(in sourceSpan, position, 'l', 'i', 's', 't')
                        && IsKeywordBoundary(in sourceSpan, position + 4))
                    {
                        AddToken(TokenKind.List, position + 4);
                        continue;
                    }
                    goto default;
                
                case 'm':
                    if (IsSequence(in sourceSpan, position, 'm', 'a', 't', 'c', 'h')
                        && IsKeywordBoundary(in sourceSpan, position + 5))
                    {
                        AddToken(TokenKind.Match, position + 5);
                        continue;
                    }
                    goto default;
                
                case 'n':
                    if (IsSequence(in sourceSpan, position, 'n', 'a', 'm', 'e', 's', 'p', 'a', 'c', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 9))
                    {
                        AddToken(TokenKind.Namespace, position + 9);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'n', 'u', 'l', 'l')
                        && IsKeywordBoundary(in sourceSpan, position + 4))
                    {
                        AddToken(TokenKind.NullLiteral, position + 4);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'n', 'e', 'w')
                        && IsKeywordBoundary(in sourceSpan, position + 3))
                    {
                        AddToken(TokenKind.New, position + 3);
                        continue;
                    }
                    goto default;
                
                case 'o':
                    if (IsSequence(in sourceSpan, position, 'o', 'r')
                        && IsKeywordBoundary(in sourceSpan, position + 2))
                    {
                        AddToken(TokenKind.LogicalOrKeyword, position + 2);
                        continue;
                    }
                    goto default;
                
                case 'p':
                    if (IsSequence(in sourceSpan, position, 'p', 'r', 'o', 't', 'e', 'c', 't', 'e', 'd')
                        && IsKeywordBoundary(in sourceSpan, position + 9))
                    {
                        AddToken(TokenKind.Protected, position + 9);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'p', 'r', 'i', 'v', 'a', 't', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 7))
                    {
                        AddToken(TokenKind.Private, position + 7);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'p', 'u', 'b', 'l', 'i', 'c')
                        && IsKeywordBoundary(in sourceSpan, position + 6))
                    {
                        AddToken(TokenKind.Public, position + 6);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'p', 'r', 'i', 'n', 't')
                        && IsKeywordBoundary(in sourceSpan, position + 5))
                    {
                        AddToken(TokenKind.Identifier, position + 5);
                        continue;
                    }
                    goto default;
                
                case 'r':
                    if (IsSequence(in sourceSpan, position, 'r', 'e', 'q', 'u', 'i', 'r', 'e', '_', 'o', 'n', 'c', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 12))
                    {
                        AddToken(TokenKind.Identifier, position + 12);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'r', 'e', 'a', 'd', 'o', 'n', 'l', 'y')
                        && IsKeywordBoundary(in sourceSpan, position + 8))
                    {
                        AddToken(TokenKind.Readonly, position + 8);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'r', 'e', 't', 'u', 'r', 'n')
                        && IsKeywordBoundary(in sourceSpan, position + 6))
                    {
                        AddToken(TokenKind.Return, position + 6);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'r', 'e', 'q', 'u', 'i', 'r', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 7))
                    {
                        AddToken(TokenKind.Identifier, position + 7);
                        continue;
                    }
                    goto default;
                
                case 's':
                    if (IsSequence(in sourceSpan, position, 's', 't', 'a', 't', 'i', 'c')
                        && IsKeywordBoundary(in sourceSpan, position + 6))
                    {
                        AddToken(TokenKind.Static, position + 6);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 's', 'w', 'i', 't', 'c', 'h')
                        && IsKeywordBoundary(in sourceSpan, position + 6))
                    {
                        AddToken(TokenKind.Switch, position + 6);
                        continue;
                    }
                    goto default;
                
                case 't':
                    if (IsSequence(in sourceSpan, position, 't', 'r', 'u', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 4))
                    {
                        AddToken(TokenKind.TrueLiteral, position + 4);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 't', 'h', 'r', 'o', 'w')
                        && IsKeywordBoundary(in sourceSpan, position + 5))
                    {
                        AddToken(TokenKind.Throw, position + 5);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 't', 'r', 'a', 'i', 't')
                        && IsKeywordBoundary(in sourceSpan, position + 5))
                    {
                        AddToken(TokenKind.Trait, position + 5);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 't', 'r', 'y')
                        && IsKeywordBoundary(in sourceSpan, position + 3))
                    {
                        AddToken(TokenKind.Try, position + 3);
                        continue;
                    }
                    goto default;
                
                case 'u':
                    if (IsSequence(in sourceSpan, position, 'u', 'n', 's', 'e', 't')
                        && IsKeywordBoundary(in sourceSpan, position + 5))
                    {
                        AddToken(TokenKind.Unset, position + 5);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'u', 's', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 3))
                    {
                        AddToken(TokenKind.Use, position + 3);
                        continue;
                    }
                    goto default;
                
                case 'w':
                    if (IsSequence(in sourceSpan, position, 'w', 'h', 'i', 'l', 'e')
                        && IsKeywordBoundary(in sourceSpan, position + 5))
                    {
                        AddToken(TokenKind.While, position + 5);
                        continue;
                    }
                    goto default;
                
                case 'x':
                    if (IsSequence(in sourceSpan, position, 'x', 'o', 'r')
                        && IsKeywordBoundary(in sourceSpan, position + 3))
                    {
                        AddToken(TokenKind.LogicalXorKeyword, position + 3);
                        continue;
                    }
                    goto default;
                
                case 'y':
                    if (IsSequence(in sourceSpan, position, 'y', 'i', 'e', 'l', 'd', ' ', 'f', 'r', 'o', 'm')
                        && IsKeywordBoundary(in sourceSpan, position + 10))
                    {
                        AddToken(TokenKind.YieldFrom, position + 10);
                        continue;
                    }
                    if (IsSequence(in sourceSpan, position, 'y', 'i', 'e', 'l', 'd')
                        && IsKeywordBoundary(in sourceSpan, position + 5))
                    {
                        AddToken(TokenKind.Yield, position + 5);
                        continue;
                    }
                    goto default;
                
                // =====================
                // IDENTIFIERS / FALLBACK
                // All letter cases that don't match a keyword land here via
                // goto default. Uppercase letters, underscores, and any other
                // unrecognised characters also fall through to here.
                // =====================
                
                default:
                    var identifierEnd = GetIdentifierEnd(in sourceSpan, position);
                    
                    if (identifierEnd == -1)
                    {
                        // ran to EOF inside an identifier — emit and stop
                        AddToken(TokenKind.Identifier, sourceSpan.Length);
                        return tokens[..tokenIndex];
                    }

                    // if GetIdentifierEnd returned the same position, we have a
                    // completely unrecognised single character — mark it unknown
                    // and advance so we never stall.
                    if (identifierEnd == position)
                    {
                        MarkUnknown();
                        continue;
                    }
                    
                    AddToken(TokenKind.Identifier, identifierEnd);
                    break;
            }
        }

        return tokens[..tokenIndex];

        void AddToken(TokenKind kind, int endPosition)
        {
            if (tokens.Length <= tokenIndex)
            {
                position = int.MaxValue;
                return;
            }
            tokens[tokenIndex] = new Token()
            {
                Kind = kind,
                RangeStart = position,
                RangeEnd = endPosition
            };
            position = endPosition;
            tokenIndex++;
        }
        
        void MarkUnknown()
        {
            AddToken(TokenKind.Unknown, position + 1);
        }
    }

    /// <summary>
    /// Seeks the end of a numeric literal, handling both integers and floats.
    /// Supports decimal (3.14), and stops at any non-digit non-decimal character.
    /// </summary>
    private static int SeekEndOfNumber(in ReadOnlySpan<char> sourceSpan, int start)
    {
        var pointer    = start;
        var hasDecimal = false;

        while (pointer < sourceSpan.Length)
        {
            if (char.IsDigit(sourceSpan[pointer]))
            {
                pointer++;
                continue;
            }

            // allow a single decimal point if the character after it is a digit
            // e.g. 3.14 — but not 3. on its own (that would be concat)
            if (sourceSpan[pointer] == '.'
                && !hasDecimal
                && pointer + 1 < sourceSpan.Length
                && char.IsDigit(sourceSpan[pointer + 1]))
            {
                hasDecimal = true;
                pointer++;
                continue;
            }

            break;
        }

        return pointer;
    }

    /// <summary>
    /// Confirms that a keyword match is a complete token and not just the prefix
    /// of a longer identifier. e.g. prevents 'if' matching the start of 'ifdef',
    /// or 'for' matching the start of 'foreach'.
    /// </summary>
    /// <param name="sourceSpan">The source span to check</param>
    /// <param name="keywordEnd">The index immediately after the last character of the keyword</param>
    /// <returns>True if the character at keywordEnd cannot continue an identifier</returns>
    private static bool IsKeywordBoundary(in ReadOnlySpan<char> sourceSpan, int keywordEnd)
    {
        if (keywordEnd >= sourceSpan.Length)
            return true;
        
        char next = sourceSpan[keywordEnd];
        return !char.IsLetterOrDigit(next) && next != '_';
    }
}