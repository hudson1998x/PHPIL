using System.Reflection.Emit;

namespace PHPIL.Engine.SyntaxTree;

public partial class VariableDeclaration
{
    public LocalBuilder? Local { get; set; }
}