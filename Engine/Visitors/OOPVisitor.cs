using PHPIL.Engine.SyntaxTree.Structure.OOP;

namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitClassNode(ClassNode node, in ReadOnlySpan<char> source);
        void VisitInterfaceNode(InterfaceNode node, in ReadOnlySpan<char> source);
        void VisitTraitNode(TraitNode node, in ReadOnlySpan<char> source);
        void VisitMethodNode(MethodNode node, in ReadOnlySpan<char> source);
        void VisitPropertyNode(PropertyNode node, in ReadOnlySpan<char> source);
        void VisitNewNode(NewNode node, in ReadOnlySpan<char> source);
        void VisitInstanceOfNode(InstanceOfNode node, in ReadOnlySpan<char> source);
        void VisitStaticAccessNode(StaticAccessNode node, in ReadOnlySpan<char> source);
        void VisitParentNode(ParentNode node, in ReadOnlySpan<char> source);
        void VisitTraitUseNode(TraitUseNode node, in ReadOnlySpan<char> source);
        void VisitConstantNode(ConstantNode node, in ReadOnlySpan<char> source);
    }
}
