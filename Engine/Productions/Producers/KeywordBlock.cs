using PHPIL.Engine.Productions;

namespace PHPIL.Engine.Producers;

public class KeywordBlock : Production
{
    public override Producer Init()
    {
        return Sequence(
            AnyToken(),
            Prefab<Block>()
        );
    }
}