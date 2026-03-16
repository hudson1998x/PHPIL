using System.Reflection.Emit;

namespace PHPIL.Engine.SyntaxTree;

/// <summary>
/// Partial class containing IL emission members for <see cref="VariableDeclaration"/>.
/// </summary>
public partial class VariableDeclaration
{
    /// <summary>
    /// Gets or sets the <see cref="LocalBuilder"/> representing the local variable
    /// slot allocated for this declaration during IL emission.
    /// </summary>
    /// <remarks>
    /// This is <see langword="null"/> prior to the IL emit phase. Once the variable
    /// has been declared via <c>ILGenerator.DeclareLocal</c>, this will hold a reference
    /// to the emitted local so it can be loaded or stored in subsequent instructions.
    /// </remarks>
    public LocalBuilder? Local { get; set; }
}