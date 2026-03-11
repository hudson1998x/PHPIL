using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree.Structure;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    public void VisitObjectAccessNode(ObjectAccessNode node, in ReadOnlySpan<char> source)
    {
        // TODO: Implement object property access when classes/objects are added.
        // For now, this is a stub.
        throw new NotImplementedException("Object property access (->) is not yet implemented. This will be coming soon!");
    }
}
