using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;

namespace PHPIL.Engine.Producers;

public class Whitespace : Production
{
    public override Producer Init()
    {
        return Sequence(
            Token(TokenKind.Whitespace)
        );
    }
}
public class NewLine : Production
{
    public override Producer Init()
    {
        return Sequence(
            Token(TokenKind.NewLine)
        );
    }
}