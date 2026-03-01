using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Producers;

public class Expression : Production
{
    public ExpressionNode? Node { get; private set; }

    public override Producer Init() =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            var (node, match) = ParseExpression(tokens, source, pointer, 0);
            if (!match.Success) return new Match(false, pointer, pointer);
            Node = node;
            return match;
        };

    // ── Precedence ────────────────────────────────────────────────────────────

    private static int Precedence(TokenKind kind) => kind switch
    {
        TokenKind.LogicalOrKeyword                                          => 1,
        TokenKind.LogicalXorKeyword                                         => 2,
        TokenKind.LogicalAndKeyword                                         => 3,
        TokenKind.QuestionMark                                              => 4,
        TokenKind.NullCoalesce                                              => 5,
        TokenKind.LogicalOr                                                 => 6,
        TokenKind.LogicalAnd                                                => 7,
        TokenKind.BitwiseOr                                                 => 8,
        TokenKind.BitwiseXor                                                => 9,
        TokenKind.Ampersand                                                 => 10,
        TokenKind.ShallowEquality   or TokenKind.DeepEquality      or
        TokenKind.ShallowInequality or TokenKind.DeepInequality    or
        TokenKind.Spaceship                                                 => 11,
        TokenKind.LessThan          or TokenKind.GreaterThan        or
        TokenKind.LessThanOrEqual   or TokenKind.GreaterThanOrEqual         => 12,
        TokenKind.LeftShift         or TokenKind.RightShift                 => 13,
        TokenKind.Add               or TokenKind.Subtract          or
        TokenKind.Concat                                                    => 14,
        TokenKind.Multiply          or TokenKind.DivideBy          or
        TokenKind.Modulo                                                    => 15,
        TokenKind.Power                                                     => 16,
        _                                                                   => -1
    };

    private static bool IsRightAssociative(TokenKind kind) => kind switch
    {
        TokenKind.Power        => true,
        TokenKind.NullCoalesce => true,
        TokenKind.QuestionMark => true,
        _                      => false
    };

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int SkipTrivia(in ReadOnlySpan<Token> tokens, int pointer)
    {
        while (pointer < tokens.Length && tokens[pointer].Kind is TokenKind.Whitespace or TokenKind.NewLine)
            pointer++;
        return pointer;
    }

    // ── Climbing ──────────────────────────────────────────────────────────────

    private static (ExpressionNode? Node, Match Match) ParseExpression(
        in ReadOnlySpan<Token> tokens,
        in ReadOnlySpan<char> source,
        int pointer,
        int minPrecedence)
    {
        pointer = SkipTrivia(tokens, pointer);

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
            if (prec < minPrecedence) break;

            current++;
            current = SkipTrivia(tokens, current);

            // Ternary: condition ? then : else
            if (opKind == TokenKind.QuestionMark)
            {
                var (thenNode, thenMatch) = ParseExpression(tokens, source, current, 0);
                if (!thenMatch.Success) return (null, new Match(false, pointer, pointer));

                current = SkipTrivia(tokens, thenMatch.End);

                if (current >= tokens.Length || tokens[current].Kind != TokenKind.Colon)
                    return (null, new Match(false, pointer, pointer));

                current = SkipTrivia(tokens, current + 1);

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

    private static (ExpressionNode? Node, Match Match) ParsePrimary(
        in ReadOnlySpan<Token> tokens,
        in ReadOnlySpan<char> source,
        int pointer)
    {
        pointer = SkipTrivia(tokens, pointer);

        if (pointer >= tokens.Length)
            return (null, new Match(false, pointer, pointer));

        var kind = tokens[pointer].Kind;

        // Unary prefix operators
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

        // Prefix increment / decrement
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

        // Grouped sub-expression
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

            return ParsePostfix(tokens, source, group, closeParen + 1);
        }

        // Variable
        if (kind == TokenKind.Variable)
        {
            int end     = pointer + 1;
            var token   = tokens[pointer];

            // Postfix ++ / --
            if (end < tokens.Length && tokens[end].Kind is TokenKind.Increment or TokenKind.Decrement)
            {
                return (new UnaryOpNode
                {
                    Operator   = tokens[end].Kind,
                    Operand    = new VariableNode { Token = token, RangeStart = pointer, RangeEnd = pointer + 1 },
                    RangeStart = pointer,
                    RangeEnd   = end + 1,
                    Prefix     = false
                }, new Match(true, pointer, end + 1));
            }

            var varNode = new VariableNode { Token = token, RangeStart = pointer, RangeEnd = pointer + 1 };
            return ParsePostfix(tokens, source, varNode, pointer + 1);
        }

        // Literals
        if (kind is TokenKind.IntLiteral    or TokenKind.FloatLiteral  or
                    TokenKind.StringLiteral or TokenKind.TrueLiteral   or
                    TokenKind.FalseLiteral  or TokenKind.NullLiteral)
        {
            var litNode = new LiteralNode { Token = tokens[pointer], RangeStart = pointer, RangeEnd = pointer + 1 };
            return (litNode, new Match(true, pointer, pointer + 1));
        }

        // Identifier — bare name or function call
        if (kind == TokenKind.Identifier)
        {
            var idNode = new LiteralNode { Token = tokens[pointer], RangeStart = pointer, RangeEnd = pointer + 1 };
            return ParsePostfix(tokens, source, idNode, pointer + 1);
        }

        // Callable keywords: print, echo, require_once etc.
        if (kind is TokenKind.Print      or TokenKind.Echo        or
                    TokenKind.Include    or TokenKind.IncludeOnce  or
                    TokenKind.Require    or TokenKind.RequireOnce)
        {
            var kwNode = new LiteralNode { Token = tokens[pointer], RangeStart = pointer, RangeEnd = pointer + 1 };
            return ParsePostfix(tokens, source, kwNode, pointer + 1);
        }

        return (null, new Match(false, pointer, pointer));
    }

    // ── Postfix ───────────────────────────────────────────────────────────────

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

            if (tokens[next].Kind == TokenKind.LeftParen)
            {
                var (args, argsEnd) = ParseArgList(tokens, source, next + 1);
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

    private static (List<ExpressionNode> Args, int End) ParseArgList(
        in ReadOnlySpan<Token> tokens,
        in ReadOnlySpan<char> source,
        int pointer)
    {
        var args = new List<ExpressionNode>();

        pointer = SkipTrivia(tokens, pointer);

        if (pointer < tokens.Length && tokens[pointer].Kind == TokenKind.RightParen)
            return (args, pointer + 1);

        while (true)
        {
            pointer = SkipTrivia(tokens, pointer);

            var (argNode, argMatch) = ParseExpression(tokens, source, pointer, 0);
            if (!argMatch.Success) return (args, -1);

            args.Add(argNode!);
            pointer = SkipTrivia(tokens, argMatch.End);

            if (pointer >= tokens.Length)          return (args, -1);
            if (tokens[pointer].Kind == TokenKind.RightParen) return (args, pointer + 1);
            if (tokens[pointer].Kind == TokenKind.Comma)      { pointer++; continue; }

            return (args, -1);
        }
    }
}