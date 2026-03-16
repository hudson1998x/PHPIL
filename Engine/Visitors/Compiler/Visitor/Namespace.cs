using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Emits IL for a namespace declaration, updating the active namespace context for the
    /// duration of the block.
    /// </summary>
    /// <param name="node">The <see cref="NamespaceNode"/> representing the namespace declaration.</param>
    /// <param name="source">The original source text, used to extract the namespace name parts.</param>
    /// <remarks>
    /// The previous namespace is saved and restored after the block's statements are emitted,
    /// supporting nested or sequential namespace declarations. If the node has no inline
    /// statements (i.e. a file-scoped namespace), <c>_currentNamespace</c> is updated but not
    /// restored — subsequent declarations in the file inherit the new namespace until another
    /// namespace node is encountered.
    /// </remarks>
    public void VisitNamespaceNode(NamespaceNode node, in ReadOnlySpan<char> source)
    {
        var oldNamespace = _currentNamespace;
        
        var nameParts = new List<string>();
        if (node.Name != null)
        {
            foreach (var part in node.Name.Parts)
                nameParts.Add(part.TextValue(in source));
        }
        _currentNamespace = nameParts.Count > 0 ? string.Join("\\", nameParts) : "";

        if (node.Statements.Count > 0)
        {
            foreach (var stmt in node.Statements)
                stmt.Accept(this, source);
            
            _currentNamespace = oldNamespace;
        }
    }

    /// <summary>
    /// Registers <c>use</c> import aliases for the current compilation scope.
    /// </summary>
    /// <param name="node">The <see cref="UseNode"/> representing the use declaration.</param>
    /// <param name="source">The original source text, used to extract fully-qualified names and aliases.</param>
    /// <remarks>
    /// Each import is registered in <c>_useImports</c> keyed by its alias. If no explicit alias
    /// is declared, the last part of the fully-qualified name is used, matching PHP's default
    /// aliasing behaviour (e.g. <c>use Foo\Bar\Baz</c> aliases as <c>Baz</c>).
    /// </remarks>
    public void VisitUseNode(UseNode node, in ReadOnlySpan<char> source)
    {
        foreach (var import in node.Imports)
        {
            var fqnParts = new List<string>();
            foreach (var part in import.Name.Parts)
                fqnParts.Add(part.TextValue(in source));
            
            var fqn = string.Join("\\", fqnParts);
            var alias = import.Alias?.TextValue(in source) ?? fqnParts[^1];
            _useImports[alias] = fqn;
        }
    }

    /// <summary>
    /// Visitor stub for a qualified name node.
    /// </summary>
    /// <param name="node">The <see cref="QualifiedNameNode"/> representing the qualified name.</param>
    /// <param name="source">The original source text.</param>
    /// <remarks>
    /// Qualified name resolution is handled inline by the contexts that consume them, such as
    /// <see cref="VisitFunctionCallNode"/>, <see cref="VisitClassNode"/>, and
    /// <see cref="ResolveFQN"/>. No IL is emitted here.
    /// </remarks>
    public void VisitQualifiedNameNode(QualifiedNameNode node, in ReadOnlySpan<char> source)
    {
        // Handled in call resolution or other contexts
    }
}