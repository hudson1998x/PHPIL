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

		node.AnalysedType = node.Operator switch
		{
			TokenKind.Concat   => AnalysedType.String,
			TokenKind.Multiply => node.Left!.AnalysedType is AnalysedType.Float || node.Right!.AnalysedType is AnalysedType.Float
				? AnalysedType.Float
				: AnalysedType.Int,
			_ => AnalysedType.Mixed
		};
	}
}