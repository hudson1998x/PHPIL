using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors.SemanticAnalysis;

public partial class SemanticVisitor
{
	public void VisitBinaryOpNode(BinaryOpNode node, in ReadOnlySpan<char> source)
	{
		node.Left?.Accept(this, source);
		node.Right?.Accept(this, source);

		if (node.Right is VariableDeclaration decl)
		{
			decl.EmitValue = true;
		}

		// Note: AnalysedType is inferred by the BinaryOpNode.AnalysedType getter
		// We don't need to set it here
	}
}