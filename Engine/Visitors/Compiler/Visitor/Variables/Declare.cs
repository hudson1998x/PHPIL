using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Emits IL for a variable declaration or assignment, allocating a local slot if necessary
    /// and storing the evaluated value.
    /// </summary>
    /// <param name="node">The <see cref="VariableDeclaration"/> representing the assignment.</param>
    /// <param name="source">The original source text, used to resolve the variable name.</param>
    /// <remarks>
    /// <para>
    /// If the right-hand side is itself a <see cref="VariableDeclaration"/> (a chained assignment
    /// such as <c>$a = $b = 0</c>), the child declaration is visited recursively first, leaving
    /// its result on the stack. The current variable is then allocated or reused and the value
    /// is stored. If <c>node.EmitValue</c> is <see langword="true"/>, the value is duplicated
    /// before storing so it remains on the stack as the expression result.
    /// </para>
    /// <para>
    /// For all other right-hand sides, the value expression is evaluated (or <see langword="null"/>
    /// pushed if absent), the local is allocated or reused, and the value is boxed according to
    /// <c>node.AnalysedType</c>:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="AnalysedType.Int"/> and <see cref="AnalysedType.Mixed"/> — boxed as <see cref="int"/>.</description></item>
    ///   <item><description><see cref="AnalysedType.Float"/> — boxed as <see cref="double"/>.</description></item>
    ///   <item><description><see cref="AnalysedType.Boolean"/> — boxed as <see cref="bool"/>.</description></item>
    ///   <item><description>All other types — no boxing emitted.</description></item>
    /// </list>
    /// <para>
    /// As with chained assignments, <c>node.EmitValue</c> causes the value to be duplicated
    /// before the final <see cref="OpCodes.Stloc"/>, leaving it on the stack for the enclosing
    /// expression context.
    /// </para>
    /// </remarks>
    public void VisitVariableDeclaration(VariableDeclaration node, in ReadOnlySpan<char> source)
    {
       var varName = node.VariableName.TextValue(in source);

       if (node.VariableValue is VariableDeclaration childDeclaration)
       {
          childDeclaration.Accept(this, source);

          if (!_locals.TryGetValue(varName, out var existingLocal))
          {
             node.Local = DeclareLocal(typeof(object));
             _locals[varName] = node.Local;
          }
          else
          {
             node.Local = existingLocal;
          }

          if (node.EmitValue)
             Emit(OpCodes.Dup);

          Emit(OpCodes.Stloc, node.Local);

          return;
       }

       if (node.VariableValue is not null)
          node.VariableValue.Accept(this, source);
       else
          Emit(OpCodes.Ldnull);

       if (!_locals.TryGetValue(varName, out var existingLocal2))
       {
          node.Local = DeclareLocal(typeof(object));
          _locals[varName] = node.Local;
       }
       else
       {
          node.Local = existingLocal2;
       }

       switch (node.AnalysedType)
       {
          case AnalysedType.Int:     Emit(OpCodes.Box, typeof(int));    break;
          case AnalysedType.Float:   Emit(OpCodes.Box, typeof(double)); break;
          case AnalysedType.Boolean: Emit(OpCodes.Box, typeof(bool));   break;
          case AnalysedType.Mixed:   Emit(OpCodes.Box, typeof(int));    break;
       }

       if (node.EmitValue)
          Emit(OpCodes.Dup);

       Emit(OpCodes.Stloc, node.Local);
    }
}