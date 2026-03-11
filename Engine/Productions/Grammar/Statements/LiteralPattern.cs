using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.OOP;


namespace PHPIL.Engine.Productions.Patterns
{
    public class LiteralPattern : Pattern
    {
        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            var token = ctx.Peek();
            result = null;

            switch (token.Kind)
            {
                case TokenKind.IntLiteral:
                case TokenKind.FloatLiteral:
                case TokenKind.TrueLiteral:
                case TokenKind.FalseLiteral:
                case TokenKind.NullLiteral:
                case TokenKind.StringLiteral:
                    var text = token.TextValue(in ctx.Source);
                    if (token.Kind == TokenKind.StringLiteral && text.StartsWith("\""))
                    {
                        if (TryParseInterpolatedString(ref ctx, out var interpolated))
                        {
                            result = interpolated;
                            return true;
                        }
                    }

                    result = new LiteralNode
                    {
                        Token = ctx.Consume(),
                        RangeStart = ctx.Position - 1,
                        RangeEnd = ctx.Position
                    };
                    return true;


                default:
                    return false;
            }
        }

        private bool TryParseInterpolatedString(ref ParserContext ctx, out SyntaxNode? result)
        {
            var token = ctx.Consume();
            var fullText = token.TextValue(in ctx.Source);
            var content = fullText.Substring(1, fullText.Length - 2); // Remove quotes
            
            var parts = new List<ExpressionNode>();
            int i = 0;
            while (i < content.Length)
            {
                int nextVar = content.IndexOf('$', i);
                int nextBrace = content.IndexOf('{', i);
                
                int next = -1;
                if (nextVar != -1 && nextBrace != -1) next = Math.Min(nextVar, nextBrace);
                else if (nextVar != -1) next = nextVar;
                else if (nextBrace != -1) next = nextBrace;

                if (next == -1)
                {
                    // Rest is literal
                    parts.Add(new LiteralNode { Token = new Token { Kind = TokenKind.StringLiteral, RangeStart = token.RangeStart + 1 + i, RangeEnd = token.RangeStart + 1 + content.Length } });

                    break;
                }

                if (next > i)
                {
                    // Text before the interpolation
                    parts.Add(new LiteralNode { Token = new Token { Kind = TokenKind.StringLiteral, RangeStart = token.RangeStart + 1 + i, RangeEnd = token.RangeStart + 1 + next } });

                }

                if (content[next] == '{' && next + 1 < content.Length && content[next+1] == '$')
                {
                    // Complex: {$var} or {$var->prop}
                    int closeBrace = content.IndexOf('}', next);
                    if (closeBrace != -1)
                    {
                        var exprText = content.Substring(next + 1, closeBrace - next - 1);
                        var innerTokens = Lexer.ParseSpan(exprText.AsSpan());
                        
                        // Adjust token ranges to be relative to the original source
                        int offset = token.RangeStart + 1 + next + 1;
                        for (int t = 0; t < innerTokens.Length; t++)
                        {
                            innerTokens[t].RangeStart += offset;
                            innerTokens[t].RangeEnd += offset;
                        }

                        var innerCtx = new ParserContext(innerTokens.AsSpan(), ctx.Source);
                        var innerExpr = Parser.ParseSingle(ref innerCtx);
                        if (innerExpr is ExpressionNode en) parts.Add(en);
                        i = closeBrace + 1;
                        continue;
                    }
                }

                
                if (content[next] == '$')
                {
                    // Simple: $var or $var->prop
                    int varEnd = Lexer.GetIdentifierEnd(content.AsSpan(), next);
                    var varName = content.Substring(next, varEnd - next);
                    
                    ExpressionNode varNode = new VariableNode { Token = new Token { Kind = TokenKind.Variable, RangeStart = token.RangeStart + 1 + next, RangeEnd = token.RangeStart + 1 + varEnd } };

                    
                    // Check for -> property access
                    if (varEnd + 2 < content.Length && content[varEnd] == '-' && content[varEnd + 1] == '>')
                    {
                        int propEnd = Lexer.GetIdentifierEnd(content.AsSpan(), varEnd + 2);
                        // Property access via "->" is not yet modeled in this interpolation path.
                        // Fall back to treating the base variable as the interpolated part.
                        parts.Add(varNode);
                        i = varEnd;
                        continue;
                    }

                    parts.Add(varNode);
                    i = varEnd;
                    continue;
                }

                i = next + 1;
            }

            if (parts.Count == 1 && parts[0] is LiteralNode)
            {
                result = parts[0];
                return true;
            }

            result = new InterpolatedStringNode { Parts = parts };
            return true;
        }

    }
}

namespace PHPIL.Engine.Productions
{
    public static partial class Grammar
    {
        public static LiteralPattern Literal()
        {
            return new LiteralPattern();
        }
    }
}
