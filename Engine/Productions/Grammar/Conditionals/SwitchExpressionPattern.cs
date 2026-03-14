using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class SwitchExpressionPattern : Pattern
{
    private void SkipTrivia(ref ParserContext ctx)
    {
        while (!ctx.IsAtEnd && (ctx.Peek().Kind == TokenKind.Whitespace || ctx.Peek().Kind == TokenKind.NewLine))
            ctx.Consume();
    }

    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        result = null;
        int start = ctx.Save();

        if (ctx.Peek().Kind != TokenKind.Switch) return false;
        ctx.Consume();
        SkipTrivia(ref ctx);

        if (ctx.Peek().Kind != TokenKind.LeftParen) { ctx.Restore(start); return false; }
        ctx.Consume();
        SkipTrivia(ref ctx);

        if (!new InnerExpressionPattern(0).TryMatch(ref ctx, out var cond)) { ctx.Restore(start); return false; }
        SkipTrivia(ref ctx);

        if (ctx.Peek().Kind != TokenKind.RightParen) { ctx.Restore(start); return false; }
        ctx.Consume();
        SkipTrivia(ref ctx);

        if (ctx.Peek().Kind != TokenKind.LeftBrace) { ctx.Restore(start); return false; }
        ctx.Consume();
        SkipTrivia(ref ctx);

        var cases = new List<CaseNode>();
        BlockNode? defaultBody = null;

        while (!ctx.IsAtEnd && ctx.Peek().Kind != TokenKind.RightBrace)
        {
            SkipTrivia(ref ctx);
            if (ctx.IsAtEnd || ctx.Peek().Kind == TokenKind.RightBrace) break;

            if (ctx.Peek().Kind == TokenKind.Case)
            {
                ctx.Consume();
                SkipTrivia(ref ctx);

                ExpressionNode? caseExpr = null;
                if (ctx.Peek().Kind != TokenKind.Colon)
                {
                    if (!new InnerExpressionPattern(0).TryMatch(ref ctx, out var caseExprNode))
                        break;
                    caseExpr = caseExprNode as ExpressionNode;
                }
                SkipTrivia(ref ctx);

                if (ctx.Peek().Kind != TokenKind.Colon)
                    break;
                ctx.Consume();
                SkipTrivia(ref ctx);

                var body = new BlockNode();
                while (!ctx.IsAtEnd && ctx.Peek().Kind != TokenKind.Case && ctx.Peek().Kind != TokenKind.Default && ctx.Peek().Kind != TokenKind.RightBrace)
                {
                    SkipTrivia(ref ctx);
                    if (ctx.IsAtEnd) break;
                    if (ctx.Peek().Kind == TokenKind.Case || ctx.Peek().Kind == TokenKind.Default || ctx.Peek().Kind == TokenKind.RightBrace)
                        break;

                    var stmt = Parser.ParseSingle(ref ctx);
                    if (stmt != null)
                        body.Statements.Add(stmt);
                    else if (!ctx.IsAtEnd)
                        ctx.Consume(); // panic recovery: skip unrecognised token to avoid infinite loop
                }

                cases.Add(new CaseNode { Expression = caseExpr, Body = body });
                continue;
            }

            if (ctx.Peek().Kind == TokenKind.Default)
            {
                ctx.Consume();
                SkipTrivia(ref ctx);

                if (ctx.Peek().Kind != TokenKind.Colon)
                    break;
                ctx.Consume();
                SkipTrivia(ref ctx);

                defaultBody = new BlockNode();
                while (!ctx.IsAtEnd && ctx.Peek().Kind != TokenKind.Case && ctx.Peek().Kind != TokenKind.RightBrace)
                {
                    SkipTrivia(ref ctx);
                    if (ctx.IsAtEnd) break;
                    if (ctx.Peek().Kind == TokenKind.Case || ctx.Peek().Kind == TokenKind.RightBrace)
                        break;

                    var stmt = Parser.ParseSingle(ref ctx);
                    if (stmt != null)
                        defaultBody.Statements.Add(stmt);
                    else if (!ctx.IsAtEnd)
                        ctx.Consume(); // panic recovery: skip unrecognised token to avoid infinite loop
                }
                continue;
            }

            break;
        }

        SkipTrivia(ref ctx);
        if (ctx.Peek().Kind == TokenKind.RightBrace)
            ctx.Consume();

        result = new SwitchNode
        {
            Expression = cond as ExpressionNode,
            Cases = cases,
            Default = defaultBody
        };

        return true;
    }
}