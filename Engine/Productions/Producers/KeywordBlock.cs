using PHPIL.Engine.Productions;

namespace PHPIL.Engine.Producers;

/// <summary>
/// Matches the minimal pattern of a keyword immediately followed by a
/// brace-delimited block — no parenthesised expression in between.
///
/// <para>
/// This covers constructs like <c>else { ... }</c> and <c>finally { ... }</c>
/// where the keyword stands alone with no condition. It intentionally has no
/// <see cref="Node"/> property because the result is typically consumed by a
/// parent production (e.g. <c>IfExpression</c>) that folds it into its own node,
/// rather than being emitted as a standalone statement.
/// </para>
///
/// <para>
/// Like <see cref="KeywordExpressionBlock"/>, the keyword is consumed with
/// <c>AnyToken()</c> — by the time this production runs, the dispatcher has
/// already confirmed which keyword it is, so re-checking the kind here would
/// be redundant.
/// </para>
/// </summary>
public class KeywordBlock : Production
{
    public override Producer Init()
    {
        return Sequence(
            AnyToken(),      // Consume the keyword — kind already validated by the caller.
            Prefab<Block>()  // Match the brace-delimited body via the shared Block production.
        );
    }
}