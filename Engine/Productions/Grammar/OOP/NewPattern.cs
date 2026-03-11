using System;
using System.Collections.Generic;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.OOP;
using PHPIL.Engine.Productions.Patterns;

namespace PHPIL.Engine.Productions
{
    public class NewPattern : Pattern
    {
        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            result = null;
            int start = ctx.Save();

            if (ctx.Peek().Kind != TokenKind.New)
            {
                return false;
            }

            ctx.Consume(); // new
            Parser.SkipTrivia(ref ctx);

            // Class name can be a QualifiedName or a Variable
            ExpressionNode? classIdentifier = null;
            if (Grammar.QualifiedName().TryMatch(ref ctx, out var qNameNode) && qNameNode is QualifiedNameNode qName)
            {
                classIdentifier = qName;
            }
            else if (ctx.Peek().Kind == TokenKind.Variable)
            {
                classIdentifier = new VariableNode { Token = ctx.Consume() };
            }

            if (classIdentifier == null)
            {
                ctx.Restore(start);
                return false;
            }

            Parser.SkipTrivia(ref ctx);

            var arguments = new List<SyntaxNode>();
            if (ctx.Peek().Kind == TokenKind.LeftParen)
            {
                if (Grammar.ArgumentList().TryMatch(ref ctx, out var argListNode) && argListNode is ArgumentListNode argList)
                {
                    arguments.AddRange(argList.Arguments);
                }
                else
                {
                    ctx.Restore(start);
                    return false;
                }
            }

            result = new NewNode { ClassIdentifier = classIdentifier, Arguments = arguments };
            return true;
        }
    }
}
