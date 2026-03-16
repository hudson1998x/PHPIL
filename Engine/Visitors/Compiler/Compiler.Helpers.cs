using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.OOP;
using PHPIL.Engine.Runtime;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Emits a boxing instruction for the value on the stack if <paramref name="node"/> is
    /// an unboxed literal type.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> whose token kind determines whether boxing is required.</param>
    /// <remarks>
    /// Only <see cref="TokenKind.IntLiteral"/>, <see cref="TokenKind.FloatLiteral"/>,
    /// <see cref="TokenKind.TrueLiteral"/>, and <see cref="TokenKind.FalseLiteral"/> produce
    /// unboxed values from <see cref="VisitLiteralNode"/> and therefore require boxing. String
    /// and null literals are reference types and are left unchanged.
    /// </remarks>
    private void EmitBoxingIfLiteral(SyntaxNode? node)
    {
        if (node is LiteralNode literal)
        {
            if (literal.Token.Kind == TokenKind.IntLiteral)
            {
                Emit(OpCodes.Box, typeof(int));
            }
            else if (literal.Token.Kind == TokenKind.FloatLiteral)
            {
                Emit(OpCodes.Box, typeof(double));
            }
            else if (literal.Token.Kind == TokenKind.TrueLiteral || literal.Token.Kind == TokenKind.FalseLiteral)
            {
                Emit(OpCodes.Box, typeof(bool));
            }
        }
    }

    /// <summary>
    /// Emits a boxing instruction for the value on the stack based on the runtime shape of
    /// <paramref name="node"/>, dispatching to <see cref="EmitBoxingIfLiteral"/> for literals
    /// and <see cref="EmitBoxingForBinaryOp"/> for binary expressions.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> whose shape determines the boxing strategy.</param>
    private void EmitBoxing(SyntaxNode? node)
    {
        if (node is LiteralNode literal)
        {
            EmitBoxingIfLiteral(literal);
        }
        else if (node is BinaryOpNode binOp)
        {
            EmitBoxingForBinaryOp(binOp);
        }
    }

    /// <summary>
    /// Emits a boxing instruction for the result of a binary expression, based on the
    /// operator's result type.
    /// </summary>
    /// <param name="node">The <see cref="BinaryOpNode"/> whose operator determines the result type.</param>
    /// <remarks>
    /// Arithmetic operators (<c>+</c>, <c>-</c>, <c>*</c>, <c>/</c>, <c>%</c>, <c>**</c>)
    /// produce an unboxed <see cref="int"/> and are boxed accordingly. Concatenation (<c>.</c>)
    /// produces a <see cref="string"/>, which is already a reference type and requires no boxing.
    /// </remarks>
    private void EmitBoxingForBinaryOp(BinaryOpNode node)
    {
        if (node.Operator is TokenKind.Add or TokenKind.Subtract or TokenKind.Multiply or 
            TokenKind.DivideBy or TokenKind.Modulo or TokenKind.Power)
        {
            Emit(OpCodes.Box, typeof(int));
        }
        else if (node.Operator is TokenKind.Concat)
        {
            // Strings are already reference types, no boxing needed
        }
    }

    /// <summary>
    /// Emits a call to <c>Runtime.CoerceToBool</c>, converting the value on top of the stack
    /// to a <see cref="bool"/> using PHP's truthiness rules.
    /// </summary>
    private void EmitCoerceToBool()
    {
        var method = typeof(Runtime.Runtime).GetMethod("CoerceToBool", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;
        Emit(OpCodes.Call, method);
    }

    /// <summary>
    /// Attempts to autoload a PHP function by name and return its <see cref="PhpFunction"/>
    /// descriptor if autoloading succeeds.
    /// </summary>
    /// <param name="name">The unqualified or fully-qualified function name to autoload.</param>
    /// <returns>
    /// The <see cref="PhpFunction"/> registered under <paramref name="name"/> after autoloading,
    /// or <see langword="null"/> if autoloading did not register the function.
    /// </returns>
    private PhpFunction? TryAutoloadFunction(string name)
    {
        if (Runtime.Runtime.AutoloadFunction(name))
        {
            return FunctionTable.GetFunction(name);
        }
        return null;
    }

    /// <summary>
    /// Resolves a callee expression to a <see cref="PhpFunction"/> descriptor, applying
    /// namespace qualification, use-import aliasing, and autoloading as fallbacks.
    /// </summary>
    /// <param name="callee">The <see cref="ExpressionNode"/> representing the function being called.</param>
    /// <param name="source">The original source text, used to extract identifier and name part text.</param>
    /// <returns>
    /// The resolved <see cref="PhpFunction"/>, or <see langword="null"/> if the callee is an
    /// instance method, static method, or cannot be resolved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Resolution proceeds as follows:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       <b><see cref="IdentifierNode"/></b> — the name is qualified with
    ///       <c>_currentNamespace</c> and looked up in <c>FunctionTable</c>. If not found,
    ///       a global (unqualified) lookup is attempted, followed by autoloading.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b><see cref="QualifiedNameNode"/></b> — fully-qualified names are used as-is;
    ///       unqualified names are resolved against <c>_useImports</c> first, then prefixed
    ///       with <c>_currentNamespace</c>. A single-part unqualified name falls back to a
    ///       global lookup if the namespaced lookup fails. Autoloading is attempted last.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b><see cref="ObjectAccessNode"/> and <see cref="StaticAccessNode"/></b> —
    ///       returned as <see langword="null"/>; instance and static method calls are resolved
    ///       dynamically at the call site in <see cref="VisitFunctionCallNode"/>.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    private PhpFunction? ResolveFunction(ExpressionNode? callee, in ReadOnlySpan<char> source)
    {
        if (callee is IdentifierNode identifierNode)
        {
            var name = identifierNode.Token.TextValue(in source);
            var fqn = string.IsNullOrEmpty(_currentNamespace) ? name : _currentNamespace + "\\" + name;
            var phpFunc = FunctionTable.GetFunction(fqn);
            if (phpFunc == null && !string.IsNullOrEmpty(_currentNamespace))
            {
                phpFunc = FunctionTable.GetFunction(name);
            }
            
            // Try autoload if not found
            if (phpFunc == null)
            {
                phpFunc = TryAutoloadFunction(name);
            }
            return phpFunc;
        }
        else if (callee is QualifiedNameNode qnameNode)
        {
            var fqnParts = new List<string>();
            foreach (var p in qnameNode.Parts)
                fqnParts.Add(p.TextValue(in source));

            string fqn = "";
            if (qnameNode.IsFullyQualified)
            {
                fqn = string.Join("\\", fqnParts);
            }
            else
            {
                if (_useImports.TryGetValue(fqnParts[0], out var imported))
                {
                    fqn = imported;
                    if (fqnParts.Count > 1) fqn += "\\" + string.Join("\\", fqnParts.Skip(1));
                }
                else
                {
                    fqn = string.IsNullOrEmpty(_currentNamespace) ? string.Join("\\", fqnParts) : _currentNamespace + "\\" + string.Join("\\", fqnParts);
                }
            }
            var phpFunc = FunctionTable.GetFunction(fqn);
            if (phpFunc == null && !qnameNode.IsFullyQualified && fqnParts.Count == 1 && !string.IsNullOrEmpty(_currentNamespace))
            {
                phpFunc = FunctionTable.GetFunction(fqnParts[0]); // Global fallback for single part
            }
            
            // Try autoload if not found
            if (phpFunc == null)
            {
                phpFunc = TryAutoloadFunction(fqn);
            }
            return phpFunc;
        }
        else
        {
            ObjectAccessNode? objectAccess = callee as ObjectAccessNode;
            if (objectAccess != null)
            {
                // For now, we can't fully resolve the method at compile time without type inference.
                // We'll need to emit a dynamic call or find a way to verify the type.
                // If it's $this, we know the type!
                return null; // Return null for now, handle in ResolveParamsAndCall
            }
            else if (callee is StaticAccessNode staticAccess)
            {
                return null; // Handle in ResolveParamsAndCall
            }
            return null;
        }
    }
}