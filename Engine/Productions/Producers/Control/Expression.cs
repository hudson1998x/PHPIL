using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Producers;

/// <summary>
/// Parses a PHP expression of any complexity and produces an <see cref="ExpressionNode"/>
/// representing its structure as a tree of binary operations, unary operations,
/// function calls, literals, variables, and groupings.
///
/// <para>
/// The implementation uses a classic <em>Pratt parser</em> (also known as
/// precedence climbing). Rather than encoding operator precedence into the grammar
/// via deeply nested rules — one rule per precedence level — a single recursive
/// function climbs the precedence table dynamically. This keeps the code flat and
/// makes adding or reordering operators trivial: just update <see cref="Precedence"/>
/// and <see cref="IsRightAssociative"/>.
/// </para>
///
/// <para>
/// The parse is split into three mutually cooperative methods:
/// <list type="bullet">
///   <item><see cref="ParseExpression"/> — the climbing loop; handles binary operators and ternary.</item>
///   <item><see cref="ParsePrimary"/> — parses a single atomic value or prefix expression.</item>
///   <item><see cref="ParsePostfix"/> — wraps a primary in any trailing call/index chains.</item>
/// </list>
/// </para>
/// </summary>
public class Expression : Production
{
    /// <summary>
    /// The AST node produced on a successful match.
    /// <c>null</c> until <see cref="Init"/> fires successfully.
    /// </summary>
    public ExpressionNode? Node { get; private set; }

    public override Producer Init() =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            // Entry point into the Pratt parser. minPrecedence = 0 means "accept any
            // operator", giving us the full expression rather than a sub-expression.
            var (node, match) = ParseExpression(tokens, source, pointer, 0);
            if (!match.Success) return new Match(false, pointer, pointer);
            Node = node;
            return match;
        };

    // ── Precedence ────────────────────────────────────────────────────────────
    // These two methods encode the PHP operator precedence table. Lower numbers
    // bind more loosely (evaluated last); higher numbers bind more tightly.
    // The ordering intentionally matches PHP's own precedence rules so that
    // `$a + $b * $c` parses as `$a + ($b * $c)`.

    /// <summary>
    /// Returns the precedence level of a binary operator token, or <c>-1</c> if
    /// the token is not a binary operator. A return value of <c>-1</c> causes the
    /// climbing loop to stop, treating the token as the end of the expression.
    /// </summary>
    private static int Precedence(TokenKind kind) => kind switch
    {
        TokenKind.LogicalOrKeyword                                          => 1,  // or
        TokenKind.LogicalXorKeyword                                         => 2,  // xor
        TokenKind.LogicalAndKeyword                                         => 3,  // and
        TokenKind.QuestionMark                                              => 4,  // ?:  ternary
        TokenKind.NullCoalesce                                              => 5,  // ??
        TokenKind.LogicalOr                                                 => 6,  // ||
        TokenKind.LogicalAnd                                                => 7,  // &&
        TokenKind.BitwiseOr                                                 => 8,  // |
        TokenKind.BitwiseXor                                                => 9,  // ^
        TokenKind.Ampersand                                                 => 10, // &
        TokenKind.ShallowEquality   or TokenKind.DeepEquality      or
        TokenKind.ShallowInequality or TokenKind.DeepInequality    or
        TokenKind.Spaceship                                                 => 11, // == === != !== <=>
        TokenKind.LessThan          or TokenKind.GreaterThan        or
        TokenKind.LessThanOrEqual   or TokenKind.GreaterThanOrEqual         => 12, // < > <= >=
        TokenKind.LeftShift         or TokenKind.RightShift                 => 13, // << >>
        TokenKind.Add               or TokenKind.Subtract          or
        TokenKind.Concat                                                    => 14, // + - .
        TokenKind.Multiply          or TokenKind.DivideBy          or
        TokenKind.Modulo                                                    => 15, // * / %
        TokenKind.Power                                                     => 16, // **
        _                                                                   => -1  // not a binary op
    };

    /// <summary>
    /// Returns whether an operator is right-associative. For left-associative
    /// operators (the majority), the climbing loop uses <c>prec + 1</c> as the
    /// minimum for the right-hand side, ensuring <c>a - b - c</c> parses as
    /// <c>(a - b) - c</c>. For right-associative operators it uses <c>prec</c>
    /// itself, so <c>a ** b ** c</c> parses as <c>a ** (b ** c)</c>.
    /// </summary>
    private static bool IsRightAssociative(TokenKind kind) => kind switch
    {
        TokenKind.Power        => true, // **  right-associative in PHP
        TokenKind.NullCoalesce => true, // ??  right-associative in PHP
        TokenKind.QuestionMark => true, // ?:  ternary is right-associative
        _                      => false
    };

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Advances <paramref name="pointer"/> past whitespace and newline tokens.
    /// Called liberally between every sub-parse so the expression parser is
    /// insensitive to formatting — multiline expressions, extra spaces, etc.
    /// </summary>
    private static int SkipTrivia(in ReadOnlySpan<Token> tokens, int pointer)
    {
        while (pointer < tokens.Length && tokens[pointer].Kind is TokenKind.Whitespace or TokenKind.NewLine)
            pointer++;
        return pointer;
    }

    // ── Climbing ──────────────────────────────────────────────────────────────

    /// <summary>
    /// The core of the Pratt parser. Parses a primary, then loops consuming
    /// binary operators whose precedence is at least <paramref name="minPrecedence"/>,
    /// recursing for each right-hand side at the appropriate minimum precedence to
    /// enforce associativity. Returns as soon as it encounters a token that isn't a
    /// binary operator or whose precedence falls below the current minimum.
    ///
    /// <para>
    /// The <paramref name="minPrecedence"/> parameter is what makes the climbing
    /// work: the top-level call uses 0 (accept everything), while recursive calls
    /// for right-hand sides use <c>prec</c> or <c>prec + 1</c> depending on
    /// associativity, naturally grouping tighter operators before looser ones without
    /// any explicit grammar nesting.
    /// </para>
    /// </summary>
    private static (ExpressionNode? Node, Match Match) ParseExpression(
        in ReadOnlySpan<Token> tokens,
        in ReadOnlySpan<char> source,
        int pointer,
        int minPrecedence)
    {
        pointer = SkipTrivia(tokens, pointer);

        // Parse the left-hand side — must succeed or the whole expression fails.
        var (lhsNode, lhsMatch) = ParsePrimary(tokens, source, pointer);
        if (!lhsMatch.Success) return (null, new Match(false, pointer, pointer));

        int current          = SkipTrivia(tokens, lhsMatch.End);
        ExpressionNode? left = lhsNode;

        while (true)
        {
            current = SkipTrivia(tokens, current);
            if (current >= tokens.Length) break;

            var opKind = tokens[current].Kind;
            int prec   = Precedence(opKind);

            // Stop climbing if this token isn't a binary operator or binds too loosely.
            // The caller at the previous recursion level will handle it instead.
            if (prec < minPrecedence) break;

            current++; // consume the operator token
            current = SkipTrivia(tokens, current);

            // Ternary is handled separately because it has three operands rather than
            // two, and needs a `:` colon separator in the middle that a standard binary
            // parse loop can't accommodate.
            if (opKind == TokenKind.QuestionMark)
            {
                // `then` branch — parse at minPrecedence 0 to allow any expression,
                // including another ternary (right-associativity handles nesting).
                var (thenNode, thenMatch) = ParseExpression(tokens, source, current, 0);
                if (!thenMatch.Success) return (null, new Match(false, pointer, pointer));

                current = SkipTrivia(tokens, thenMatch.End);

                // The `:` separator is mandatory — absence means malformed ternary.
                if (current >= tokens.Length || tokens[current].Kind != TokenKind.Colon)
                    return (null, new Match(false, pointer, pointer));

                current = SkipTrivia(tokens, current + 1);

                // `else` branch — again at minPrecedence 0.
                var (elseNode, elseMatch) = ParseExpression(tokens, source, current, 0);
                if (!elseMatch.Success) return (null, new Match(false, pointer, pointer));

                left = new TernaryNode
                {
                    Condition  = left,
                    Then       = thenNode,
                    Else       = elseNode,
                    RangeStart = pointer,
                    RangeEnd   = elseMatch.End
                };

                current = elseMatch.End;
                continue;
            }

            // For all standard binary operators: recurse for the RHS at a minimum
            // precedence that enforces the correct associativity.
            // Right-associative: use the same precedence so a chain like `a ** b ** c`
            //   keeps recursing rightward → `a ** (b ** c)`.
            // Left-associative: use prec + 1 so a chain like `a + b + c` stops the
            //   recursion early and folds left → `(a + b) + c`.
            int nextMin             = IsRightAssociative(opKind) ? prec : prec + 1;
            var (rhsNode, rhsMatch) = ParseExpression(tokens, source, current, nextMin);
            if (!rhsMatch.Success) return (null, new Match(false, pointer, pointer));

            left = new BinaryOpNode
            {
                Left       = left,
                Operator   = opKind,
                Right      = rhsNode,
                RangeStart = pointer,
                RangeEnd   = rhsMatch.End
            };

            current = rhsMatch.End;
        }

        return (left, new Match(true, pointer, current));
    }

    // ── Primary ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a single atomic expression — a literal, variable, grouped
    /// sub-expression, or a prefix unary operator applied to any of the above.
    /// After successfully parsing the atom, hands off to <see cref="ParsePostfix"/>
    /// to chain any trailing call or index operations.
    ///
    /// <para>
    /// Prefix <c>++</c>/<c>--</c> and postfix <c>++</c>/<c>--</c> are both handled
    /// here rather than in the climbing loop because they bind more tightly than any
    /// binary operator — they're effectively part of the primary, not a binary
    /// operation between two sub-expressions.
    /// </para>
    /// </summary>
    private static (ExpressionNode? Node, Match Match) ParsePrimary(
        in ReadOnlySpan<Token> tokens,
        in ReadOnlySpan<char> source,
        int pointer)
    {
        pointer = SkipTrivia(tokens, pointer);

        if (pointer >= tokens.Length)
            return (null, new Match(false, pointer, pointer));

        var kind = tokens[pointer].Kind;

        // Unary prefix: `-`, `!`, `~` — recursively parse the operand as another
        // primary so that `-(-$x)` and `!true` both work naturally.
        if (kind is TokenKind.Subtract or TokenKind.Not or TokenKind.BitwiseNot)
        {
            var (operandNode, operandMatch) = ParsePrimary(tokens, source, pointer + 1);
            if (!operandMatch.Success) return (null, new Match(false, pointer, pointer));

            return (new UnaryOpNode
            {
                Operator   = kind,
                Operand    = operandNode,
                RangeStart = pointer,
                RangeEnd   = operandMatch.End,
                Prefix     = true
            }, new Match(true, pointer, operandMatch.End));
        }

        // Prefix increment / decrement: `++$x`, `--$x`.
        // Separated from the general unary block above for clarity — these operators
        // only make sense applied to an lvalue, though enforcing that is left to
        // a later semantic analysis pass rather than the parser.
        if (kind is TokenKind.Increment or TokenKind.Decrement)
        {
            var (operandNode, operandMatch) = ParsePrimary(tokens, source, pointer + 1);
            if (!operandMatch.Success) return (null, new Match(false, pointer, pointer));

            return (new UnaryOpNode
            {
                Operator   = kind,
                Operand    = operandNode,
                RangeStart = pointer,
                RangeEnd   = operandMatch.End,
                Prefix     = true
            }, new Match(true, pointer, operandMatch.End));
        }

        // Grouped sub-expression: `( expr )`.
        // Parses the inner expression at minPrecedence 0 (full precedence range),
        // then wraps it in a GroupNode to preserve the explicit grouping in the AST
        // — useful for pretty-printing and for any downstream pass that cares whether
        // the user wrote `(a + b) * c` vs `a + b * c`.
        if (kind == TokenKind.LeftParen)
        {
            var (innerNode, innerMatch) = ParseExpression(tokens, source, pointer + 1, 0);
            if (!innerMatch.Success) return (null, new Match(false, pointer, pointer));

            int closeParen = SkipTrivia(tokens, innerMatch.End);
            if (closeParen >= tokens.Length || tokens[closeParen].Kind != TokenKind.RightParen)
                return (null, new Match(false, pointer, pointer));

            var group = new GroupNode
            {
                Inner      = innerNode,
                RangeStart = pointer,
                RangeEnd   = closeParen + 1
            };

            // Pass through postfix so `(fn)()` and `(obj)->method()` work correctly.
            return ParsePostfix(tokens, source, group, closeParen + 1);
        }

        // Variable: `$name`.
        // Postfix `++`/`--` are checked immediately here rather than in ParsePostfix
        // because they need to wrap the VariableNode specifically, and the check is
        // cheaper done inline than routing through the general postfix loop.
        if (kind == TokenKind.Variable)
        {
            int end     = pointer + 1;
            var token   = tokens[pointer];

            // Postfix increment / decrement: `$x++`, `$x--`.
            if (end < tokens.Length && tokens[end].Kind is TokenKind.Increment or TokenKind.Decrement)
            {
                return (new UnaryOpNode
                {
                    Operator   = tokens[end].Kind,
                    Operand    = new VariableNode { Token = token, RangeStart = pointer, RangeEnd = pointer + 1 },
                    RangeStart = pointer,
                    RangeEnd   = end + 1,
                    Prefix     = false  // postfix — operator comes after the operand
                }, new Match(true, pointer, end + 1));
            }

            var varNode = new VariableNode { Token = token, RangeStart = pointer, RangeEnd = pointer + 1 };
            return ParsePostfix(tokens, source, varNode, pointer + 1);
        }

        // Literals: int, float, string, true, false, null.
        // No postfix chaining needed — you can't call a literal or index into it
        // directly in PHP without first assigning it to a variable.
        if (kind is TokenKind.IntLiteral    or TokenKind.FloatLiteral  or
                    TokenKind.StringLiteral or TokenKind.TrueLiteral   or
                    TokenKind.FalseLiteral  or TokenKind.NullLiteral)
        {
            var litNode = new LiteralNode { Token = tokens[pointer], RangeStart = pointer, RangeEnd = pointer + 1 };
            return (litNode, new Match(true, pointer, pointer + 1));
        }

        // Identifier — a bare name that could be a constant or the callee of a
        // function call. Routed through ParsePostfix so `foo()` and `foo()()` both
        // work: the identifier becomes the callee and the call is wrapped around it.
        if (kind == TokenKind.Identifier)
        {
            var idNode = new LiteralNode { Token = tokens[pointer], RangeStart = pointer, RangeEnd = pointer + 1 };
            return ParsePostfix(tokens, source, idNode, pointer + 1);
        }

        // Language-level callable keywords: `echo`, `print`, `include`, etc.
        // PHP treats these as statement-level constructs but they can appear in
        // expression position too (e.g. `$x = include 'file.php'`). Represented as
        // a LiteralNode here so the callee slot of a FunctionCallNode can hold them —
        // a later semantic pass distinguishes them from user-defined function calls.
        if (kind is TokenKind.Print      or TokenKind.Echo        or
                    TokenKind.Include    or TokenKind.IncludeOnce  or
                    TokenKind.Require    or TokenKind.RequireOnce)
        {
            var kwNode = new LiteralNode { Token = tokens[pointer], RangeStart = pointer, RangeEnd = pointer + 1 };
            return ParsePostfix(tokens, source, kwNode, pointer + 1);
        }

        // Nothing matched — not an expression we recognise.
        return (null, new Match(false, pointer, pointer));
    }

    // ── Postfix ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Wraps a parsed primary in any trailing postfix operations — currently
    /// function calls (<c>callee(...)</c>). The loop continues greedily so that
    /// chained calls like <c>foo()()</c> or <c>getFactory()(args)</c> are handled
    /// correctly without recursion.
    ///
    /// <para>
    /// Additional postfix forms (array access <c>[$i]</c>, property access <c>->prop</c>,
    /// null-safe access <c>?->prop</c>) would be added here when implemented.
    /// </para>
    /// </summary>
    private static (ExpressionNode? Node, Match Match) ParsePostfix(
        in ReadOnlySpan<Token> tokens,
        in ReadOnlySpan<char> source,
        ExpressionNode current,
        int pointer)
    {
        while (true)
        {
            int next = SkipTrivia(tokens, pointer);
            if (next >= tokens.Length) break;

            // Function call: the primary is followed by `(` so it's the callee.
            // Delegate argument parsing to ParseArgList for the contents of the parens.
            if (tokens[next].Kind == TokenKind.LeftParen)
            {
                var (args, argsEnd) = ParseArgList(tokens, source, next + 1);

                // argsEnd == -1 signals a malformed argument list — stop chaining
                // rather than propagating a failure node.
                if (argsEnd < 0) break;

                current = new FunctionCallNode
                {
                    Callee     = current,
                    Args       = args,
                    RangeStart = current.RangeStart,
                    RangeEnd   = argsEnd
                };

                pointer = argsEnd;
                continue;
            }

            break;
        }

        return (current, new Match(true, current.RangeStart, pointer));
    }

    // ── Arg List ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a comma-separated list of expressions between the opening paren
    /// (already consumed by the caller) and the closing paren. Returns the
    /// collected argument nodes and the position just past the closing paren,
    /// or <c>-1</c> as the end position to signal a malformed argument list
    /// without throwing.
    ///
    /// <para>
    /// Each argument is a full expression parsed at minPrecedence 0, so default
    /// values, ternaries, and nested calls in argument position all work without
    /// special casing.
    /// </para>
    /// </summary>
    private static (List<ExpressionNode> Args, int End) ParseArgList(
        in ReadOnlySpan<Token> tokens,
        in ReadOnlySpan<char> source,
        int pointer)
    {
        var args = new List<ExpressionNode>();

        pointer = SkipTrivia(tokens, pointer);

        // Empty argument list — closing paren immediately after opening paren.
        if (pointer < tokens.Length && tokens[pointer].Kind == TokenKind.RightParen)
            return (args, pointer + 1);

        while (true)
        {
            pointer = SkipTrivia(tokens, pointer);

            var (argNode, argMatch) = ParseExpression(tokens, source, pointer, 0);
            if (!argMatch.Success) return (args, -1); // malformed argument

            args.Add(argNode!);
            pointer = SkipTrivia(tokens, argMatch.End);

            if (pointer >= tokens.Length)                              return (args, -1); // unclosed paren
            if (tokens[pointer].Kind == TokenKind.RightParen)         return (args, pointer + 1); // done
            if (tokens[pointer].Kind == TokenKind.Comma)              { pointer++; continue; }     // next arg

            // Anything else (e.g. a stray operator) means the arg list is malformed.
            return (args, -1);
        }
    }
}