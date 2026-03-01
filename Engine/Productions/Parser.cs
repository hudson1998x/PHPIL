using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Producers;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions;

public static class Parser
{
    public static SyntaxNode Parse(in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int startPosition = 0)
    {
        var root    = new BlockNode();
        int pointer = startPosition;

        while (pointer < tokens.Length)
        {
            var token = tokens[pointer];

            if (token.Kind is TokenKind.Whitespace       or TokenKind.NewLine
                           or TokenKind.SingleLineComment or TokenKind.MultiLineComment
                           or TokenKind.PhpOpenTag        or TokenKind.PhpCloseTag
                           or TokenKind.ExpressionTerminator)
            {
                pointer++;
                continue;
            }

            var node = TryProduce(tokens, source, token.Kind, ref pointer);

            if (node is not null)
                root.Statements.Add(node);
            else
                pointer++;
        }

        return root;
    }

    public static SyntaxNode? TryProduce(
        in ReadOnlySpan<Token> tokens,
        in ReadOnlySpan<char> source,
        TokenKind kind,
        ref int pointer)
    {
        switch (kind)
        {
            case TokenKind.If:
            {
                var production = new IfExpression();
                var match      = production.Init()(tokens, source, pointer);
                if (match.Success) { pointer = match.End; return production.Node; }
                break;
            }

            case TokenKind.While:
            case TokenKind.For:
            case TokenKind.Foreach:
            {
                var production = new KeywordExpressionBlock();
                var match      = production.Init()(tokens, source, pointer);
                if (match.Success) { pointer = match.End; return production.Node; }
                break;
            }

            case TokenKind.LeftBrace:
            {
                var production = new Block();
                var match      = production.Init()(tokens, source, pointer);
                if (match.Success) { pointer = match.End; return production.Node; }
                break;
            }
            
            case TokenKind.Function:
            {
                var production = new FunctionDeclaration();
                var match      = production.Init()(tokens, source, pointer);
                if (match.Success) { pointer = match.End; return production.Node; }
                break;
            }
            
            case TokenKind.Variable:
            {
                // Try variable assignment first
                var assignment = new VariableAssignment();
                var assignMatch = assignment.Init()(tokens, source, pointer);

                if (assignMatch.Success)
                {
                    pointer = assignMatch.End;
                    return assignment.Node;
                }

                // Fallback to expression if not assignment
                var expression = new Expression();
                var exprMatch  = expression.Init()(tokens, source, pointer);

                if (exprMatch.Success)
                {
                    pointer = exprMatch.End;
                    return expression.Node;
                }

                break;
            }
            
            case TokenKind.Return:
            {
                var production = new ReturnStatement();
                var match      = production.Init()(tokens, source, pointer);
                if (match.Success)
                {
                    pointer = match.End;
                    return production.Node;
                }
                break;
            }

            case TokenKind.Identifier:
            case TokenKind.IntLiteral:
            case TokenKind.FloatLiteral:
            case TokenKind.StringLiteral:
            case TokenKind.TrueLiteral:
            case TokenKind.FalseLiteral:
            case TokenKind.NullLiteral:
            case TokenKind.LeftParen:
            case TokenKind.Not:
            case TokenKind.Subtract:
            case TokenKind.Increment:
            case TokenKind.Decrement:
            case TokenKind.Print:
            case TokenKind.Echo:
            case TokenKind.Include:
            case TokenKind.IncludeOnce:
            case TokenKind.Require:
            case TokenKind.RequireOnce:
            {
                var production = new Expression();
                var match      = production.Init()(tokens, source, pointer);
                if (match.Success) { pointer = match.End; return production.Node; }
                break;
            }
        }

        return null;
    }
}