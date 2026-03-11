using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
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

    public void VisitQualifiedNameNode(QualifiedNameNode node, in ReadOnlySpan<char> source)
    {
        // Handled in call resolution or other contexts
    }
}
