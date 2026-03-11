using System;
using System.Collections.Generic;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure.OOP;
using PHPIL.Engine.Productions.Patterns;

namespace PHPIL.Engine.Productions
{
    public class MethodPattern : Pattern
    {
        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            result = null;
            int start = ctx.Save();

            PhpModifiers modifiers = PhpModifiers.None;
            while (true)
            {
                var kind = ctx.Peek().Kind;
                if (kind == TokenKind.Public) modifiers |= PhpModifiers.Public;
                else if (kind == TokenKind.Protected) modifiers |= PhpModifiers.Protected;
                else if (kind == TokenKind.Private) modifiers |= PhpModifiers.Private;
                else if (kind == TokenKind.Static) modifiers |= PhpModifiers.Static;
                else if (kind == TokenKind.Final) modifiers |= PhpModifiers.Final;
                else if (kind == TokenKind.Abstract) modifiers |= PhpModifiers.Abstract;
                else break;

                ctx.Consume();
                Parser.SkipTrivia(ref ctx);
            }

            if (ctx.Peek().Kind != TokenKind.Function)
            {
                ctx.Restore(start);
                return false;
            }

            ctx.Consume(); // function
            Parser.SkipTrivia(ref ctx);

            if (ctx.Peek().Kind != TokenKind.Identifier)
            {
                ctx.Restore(start);
                return false;
            }

            var name = new IdentifierNode { Token = ctx.Consume() };
            Parser.SkipTrivia(ref ctx);

            if (ctx.Peek().Kind != TokenKind.LeftParen)
            {
                ctx.Restore(start);
                return false;
            }

            // We can reuse ParameterListPattern here
            if (!Grammar.ParameterList().TryMatch(ref ctx, out var paramListNode))
            {
                ctx.Restore(start);
                return false;
            }
            if (paramListNode is not ParameterListNode paramList)
            {
                ctx.Restore(start);
                return false;
            }
            
            Parser.SkipTrivia(ref ctx);

            IdentifierNode? returnType = null;
            if (ctx.Peek().Kind == TokenKind.Colon)
            {
                ctx.Consume();
                Parser.SkipTrivia(ref ctx);
                if (ctx.Peek().Kind == TokenKind.Identifier)
                {
                    returnType = new IdentifierNode { Token = ctx.Consume() };
                }
                Parser.SkipTrivia(ref ctx);
            }

            BlockNode? body = null;
            if (ctx.Peek().Kind == TokenKind.LeftBrace)
            {
                if (Grammar.Block().TryMatch(ref ctx, out var blockNode) && blockNode is BlockNode block)
                {
                    body = block;
                }
                else
                {
                    ctx.Restore(start);
                    return false;
                }
            }
            else if (ctx.Peek().Kind == TokenKind.ExpressionTerminator)
            {
                ctx.Consume(); // ; for abstract methods
            }
            else
            {
                ctx.Restore(start);
                return false;
            }

            // Convert FunctionParameter (from grammar) to ParameterNode (syntax tree used by compiler)
            var convertedParams = new List<ParameterNode>();
            if (paramList is ParameterListNode pln)
            {
                foreach (var p in pln.Parameters)
                {
                    var typeHint = p.TypeHint != null ? new IdentifierNode { Token = p.TypeHint.Value } : null;
                    var paramNode = new ParameterNode
                    {
                        TypeHint = typeHint,
                        Name = new IdentifierNode { Token = p.Name },
                        DefaultValue = p.DefaultValue
                    };
                    convertedParams.Add(paramNode);
                }
            }
            result = new MethodNode
            {
                Modifiers = modifiers,
                Name = name,
                Parameters = convertedParams,
                ReturnType = returnType,
                Body = body
            };
            return true;
        }
    }
}
