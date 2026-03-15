using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure.OOP;

namespace PHPIL.Engine.Productions
{
    public class PropertyPattern : Pattern
    {
        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            SyntaxNode? node = null; result = null;
            int start = ctx.Save();

            PhpModifiers modifiers = PhpModifiers.None;
            while (true)
            {
                var kind = ctx.Peek().Kind;
                if (kind == TokenKind.Public) modifiers |= PhpModifiers.Public;
                else if (kind == TokenKind.Protected) modifiers |= PhpModifiers.Protected;
                else if (kind == TokenKind.Private) modifiers |= PhpModifiers.Private;
                else if (kind == TokenKind.Static) modifiers |= PhpModifiers.Static;
                else if (kind == TokenKind.Readonly) modifiers |= PhpModifiers.Readonly;
                else break;

                ctx.Consume();
                Parser.SkipTrivia(ref ctx);
            }

            // Handle typed properties: public int $name or public ?string $name or public ?Test $name
            // Type is optional - if present, consume it
            // First check for nullable marker at the beginning
            if (ctx.Peek().Kind == TokenKind.QuestionMark)
            {
                ctx.Consume();
                Parser.SkipTrivia(ref ctx);
            }
            
            if (ctx.Peek().Kind == TokenKind.Identifier || ctx.Peek().Kind == TokenKind.Array)
            {
                var typeToken = ctx.Peek();
                var typeText = typeToken.TextValue(in ctx.Source);
                if (typeText is "int" or "string" or "float" or "bool" or "array" or "mixed")
                {
                    ctx.Consume();
                    Parser.SkipTrivia(ref ctx);
                    // Check for nullable marker after basic type
                    if (ctx.Peek().Kind == TokenKind.QuestionMark)
                    {
                        ctx.Consume();
                        Parser.SkipTrivia(ref ctx);
                    }
                }
                // Special handling for "self" - consume it but don't treat as class
                else if (typeText == "self")
                {
                    ctx.Consume();
                    Parser.SkipTrivia(ref ctx);
                    // Check for nullable marker after self
                    if (ctx.Peek().Kind == TokenKind.QuestionMark)
                    {
                        ctx.Consume();
                        Parser.SkipTrivia(ref ctx);
                    }
                }
                else
                {
                    // Try to match a qualified name (e.g., Namespace\Class or just Class)
                    // Just consume the identifier and potential namespace separators
                    ctx.Consume();
                    Parser.SkipTrivia(ref ctx);
                    while (ctx.Peek().Kind == TokenKind.NamespaceSeparator)
                    {
                        ctx.Consume(); // consume '\'
                        Parser.SkipTrivia(ref ctx);
                        if (ctx.Peek().Kind == TokenKind.Identifier)
                        {
                            ctx.Consume();
                            Parser.SkipTrivia(ref ctx);
                        }
                    }
                }
            }

            if (ctx.Peek().Kind != TokenKind.Variable)
            {
                ctx.Restore(start);
                return false;
            }

            var name = new IdentifierNode { Token = ctx.Consume() };
            Parser.SkipTrivia(ref ctx);

            SyntaxNode? defaultValue = null;
            if (ctx.Peek().Kind == TokenKind.AssignEquals)
            {
                ctx.Consume();
                Parser.SkipTrivia(ref ctx);
                if (Grammar.Expressions.Inner().TryMatch(ref ctx, out var val))
                {
                    defaultValue = val;
                }
                else
                {
                    ctx.Restore(start);
                    return false;
                }
            }

            if (ctx.Peek().Kind != TokenKind.ExpressionTerminator)
            {
                ctx.Restore(start);
                return false;
            }
            ctx.Consume(); // ;

            result = new PropertyNode { Modifiers = modifiers, Name = name, DefaultValue = defaultValue };
            return true;
        }
    }
}
