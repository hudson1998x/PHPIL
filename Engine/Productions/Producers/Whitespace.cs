using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;

namespace PHPIL.Engine.Producers;

/// <summary>
/// Matches a single whitespace token (spaces, tabs — anything the lexer
/// classified as <see cref="TokenKind.Whitespace"/>).
///
/// <para>
/// Defined as its own named production rather than an inline
/// <c>Token(TokenKind.Whitespace)</c> call so it can be referenced via
/// <c>Prefab&lt;Whitespace&gt;()</c> in rules that need to explicitly
/// account for whitespace — for example, between the keyword and the
/// opening paren of a construct, in contexts where the parser doesn't
/// skip whitespace automatically.
/// </para>
/// </summary>
public class Whitespace : Production
{
    public override Producer Init()
    {
        return Sequence(
            Token(TokenKind.Whitespace)
        );
    }
}

/// <summary>
/// Matches a single newline token as classified by the lexer.
///
/// <para>
/// Mirrors <see cref="Whitespace"/> in purpose — a named wrapper around a
/// primitive token match so rules that care about line boundaries can
/// reference it cleanly via <c>Prefab&lt;NewLine&gt;()</c>, keeping the
/// intent explicit rather than scattering raw <c>Token(TokenKind.NewLine)</c>
/// calls throughout the grammar.
/// </para>
/// </summary>
public class NewLine : Production
{
    public override Producer Init()
    {
        return Sequence(
            Token(TokenKind.NewLine)
        );
    }
}