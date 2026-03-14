using PHPIL.Engine.CodeLexer;

namespace PHPIL.Tests.Engine;

public class CodeLexerTests : BaseTest
{
	[PHPILTest]
	public void GreedyMatch_ShouldDifferentiate_Equality()
	{
		var kinds = GetKinds("= == === != !== <=> ?? ??=");
		var expected = new[]
		{
			TokenKind.AssignEquals,
			TokenKind.ShallowEquality,
			TokenKind.DeepEquality,
			TokenKind.ShallowInequality,
			TokenKind.DeepInequality,
			TokenKind.Spaceship,
			TokenKind.NullCoalesce,
			TokenKind.NullCoalesceAssign
		};
		AssertEqual(expected, kinds);
	}

	[PHPILTest]
	public void GreedyMatch_ShouldDifferentiate_Arithmetic()
	{
		var kinds = GetKinds("+ += ++ - -= -- * *= ** **= / /= . .= ...");
		var expected = new[]
		{
			TokenKind.Add, TokenKind.AddAssign, TokenKind.Increment,
			TokenKind.Subtract, TokenKind.SubtractAssign, TokenKind.Decrement,
			TokenKind.Multiply, TokenKind.MultiplyAssign, TokenKind.Power, TokenKind.PowerAssign,
			TokenKind.DivideBy, TokenKind.DivideAssign,
			TokenKind.Concat, TokenKind.ConcatAppend, TokenKind.CollectSpread
		};
		AssertEqual(expected, kinds);
	}

	[PHPILTest]
	public void Tags_ShouldLex_OpenAndClose()
	{
		var kinds = GetKinds("<?php ?>", ignoreTrivia: false);
		AssertEqual(TokenKind.PhpOpenTag, kinds[0]);
		AssertEqual(TokenKind.Whitespace, kinds[1]);
		AssertEqual(TokenKind.PhpCloseTag, kinds[2]);
	}

	[PHPILTest]
	public void Variables_ShouldHandle_ComplexNames()
	{
		var kinds = GetKinds("$var $_var $var123 $VAR_NAME");
		var expected = new[] { TokenKind.Variable, TokenKind.Variable, TokenKind.Variable, TokenKind.Variable };
		AssertEqual(expected, kinds);
	}

	[PHPILTest]
	public void Literals_ShouldLex_Numbers()
	{
		AssertEqual(TokenKind.IntLiteral, GetKinds("123")[0]);
		AssertEqual(TokenKind.FloatLiteral, GetKinds("123.456")[0]);
	}

	[PHPILTest]
	public void Literals_ShouldLex_Strings()
	{
		AssertEqual(TokenKind.StringLiteral, GetKinds("'single quoted'")[0]);
		AssertEqual(TokenKind.StringLiteral, GetKinds("\"double quoted\"")[0]);
	}

	[PHPILTest]
	public void Keywords_ShouldBe_CaseInsensitive()
	{
		var kinds = GetKinds("if else while for function return as ");
		var expected = new[]
		{
			TokenKind.If, TokenKind.Else, TokenKind.While,
			TokenKind.For, TokenKind.Function, TokenKind.Return,
			TokenKind.As
		};
		AssertEqual(expected, kinds);
	}

	[PHPILTest]
	public void LanguageConstructs_ShouldBe_Identifiers()
	{
		var kinds = GetKinds("print echo include require die");
		var expected = new[]
		{
			TokenKind.Identifier, // print
			TokenKind.Identifier, // echo
			TokenKind.Identifier, // include
			TokenKind.Require, // require
			TokenKind.Die // die
		};
		AssertEqual(expected, kinds);
	}

	[PHPILTest]
	public void Regression_PrintWithParens()
	{
		var source = "print(\"Counter: \" . $i);";
		var kinds = GetKinds(source);
		var expected = new[]
		{
			TokenKind.Identifier,
			TokenKind.LeftParen,
			TokenKind.StringLiteral,
			TokenKind.Concat,
			TokenKind.Variable,
			TokenKind.RightParen,
			TokenKind.ExpressionTerminator
		};
		AssertEqual(expected, kinds);
	}

	[PHPILTest]
	public void Regression_ForLoopHeader()
	{
		var source = "for ($i = 0; $i < 10; $i++)";
		var kinds = GetKinds(source);
		var expected = new[]
		{
			TokenKind.For,
			TokenKind.LeftParen,
			TokenKind.Variable,
			TokenKind.AssignEquals,
			TokenKind.IntLiteral,
			TokenKind.ExpressionTerminator,
			TokenKind.Variable,
			TokenKind.LessThan,
			TokenKind.IntLiteral,
			TokenKind.ExpressionTerminator,
			TokenKind.Variable,
			TokenKind.Increment,
			TokenKind.RightParen
		};
		AssertEqual(expected, kinds);
	}
}