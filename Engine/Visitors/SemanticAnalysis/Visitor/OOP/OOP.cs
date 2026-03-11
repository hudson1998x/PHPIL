using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.OOP;

namespace PHPIL.Engine.Visitors.SemanticAnalysis;

public partial class SemanticVisitor
{
    public void VisitClassNode(ClassNode node, in ReadOnlySpan<char> source)
    {
        var fqn = ResolveFQN(node.Name, source);
        var phpType = new PhpType
        {
            Name = fqn,
            Definition = node
        };
        TypeTable.RegisterType(phpType);

        // Visit members
        foreach (var member in node.Members)
            member.Accept(this, source);
    }

    public void VisitInterfaceNode(InterfaceNode node, in ReadOnlySpan<char> source)
    {
        var fqn = ResolveFQN(node.Name, source);
        var phpType = new PhpType
        {
            Name = fqn,
            InterfaceDefinition = node
        };
        TypeTable.RegisterType(phpType);

        foreach (var member in node.Members)
            member.Accept(this, source);
    }

    public void VisitTraitNode(TraitNode node, in ReadOnlySpan<char> source)
    {
        var fqn = ResolveFQN(node.Name, source);
        var phpType = new PhpType
        {
            Name = fqn,
            TraitDefinition = node
        };
        TypeTable.RegisterType(phpType);

        foreach (var member in node.Members)
            member.Accept(this, source);
    }

    public void VisitMethodNode(MethodNode node, in ReadOnlySpan<char> source)
    {
        node.Body?.Accept(this, source);
    }

    public void VisitPropertyNode(PropertyNode node, in ReadOnlySpan<char> source)
    {
        node.DefaultValue?.Accept(this, source);
    }

    public void VisitConstantNode(ConstantNode node, in ReadOnlySpan<char> source)
    {
        node.Value?.Accept(this, source);
    }

    public void VisitTraitUseNode(TraitUseNode node, in ReadOnlySpan<char> source)
    {
    }

    public void VisitNewNode(NewNode node, in ReadOnlySpan<char> source)
    {
        foreach (var arg in node.Arguments)
            arg.Accept(this, source);
    }

    public void VisitInstanceOfNode(InstanceOfNode node, in ReadOnlySpan<char> source)
    {
        node.Expression?.Accept(this, source);
    }

    public void VisitStaticAccessNode(StaticAccessNode node, in ReadOnlySpan<char> source)
    {
        node.Target?.Accept(this, source);
    }

    public void VisitParentNode(ParentNode node, in ReadOnlySpan<char> source)
    {
    }

    private string ResolveFQN(SyntaxNode node, in ReadOnlySpan<char> source)
    {
        if (node is QualifiedNameNode qname)
        {
            var parts = new List<string>();
            foreach (var p in qname.Parts)
                parts.Add(p.TextValue(in source));

            string name = string.Join("\\", parts);
            if (qname.IsFullyQualified) return name;

            return string.IsNullOrEmpty(_currentNamespace) ? name : _currentNamespace + "\\" + name;
        }
        return "";
    }
}
