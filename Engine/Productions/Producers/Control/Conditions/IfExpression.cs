using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Producers;

/// <summary>
/// Matches a complete PHP <c>if</c> statement, including any number of
/// <c>elseif</c> branches and an optional <c>else</c> branch:
/// <code>
/// if (expr) { ... } elseif (expr) { ... } else { ... }
/// </code>
/// Produces an <see cref="IfNode"/> with the condition, body, elseif chain,
/// and else block all populated.
///
/// <para>
/// Inherits from <see cref="KeywordExpressionBlock"/> which handles the
/// <c>if (expr) { body }</c> core. This class layers the optional
/// <c>elseif</c>/<c>else</c> tail on top by running the base producer first,
/// then greedily consuming as many <c>elseif</c> branches as are present before
/// attempting a single optional <c>else</c>.
/// </para>
/// </summary>
public class IfExpression : KeywordExpressionBlock
{
    /// <summary>
    /// Shadows the base <see cref="KeywordExpressionBlock.Node"/> with a typed
    /// <see cref="IfNode"/>. The <c>new</c> keyword is required because the base
    /// property returns <see cref="SyntaxNode"/> — callers that have a concrete
    /// <see cref="IfExpression"/> reference get the typed version directly without
    /// needing to cast.
    /// </summary>
    public new IfNode? Node { get; private set; }

    /// <summary>
    /// The ordered list of <c>elseif</c> branches found after the main body.
    /// Empty when no <c>elseif</c> clauses are present. Populated in-place via
    /// <c>Clear()</c> + <c>Add()</c> each time <see cref="Init"/> fires so the
    /// same <see cref="IfExpression"/> instance can be reused across parse attempts.
    /// </summary>
    public List<ElseIfNode> ElseIfs { get; } = new();

    /// <summary>
    /// The body block of the <c>else</c> branch, or <c>null</c> if no <c>else</c>
    /// clause was present.
    /// </summary>
    public BlockNode? Else { get; private set; }

    /// <summary>
    /// Called by the base class <see cref="KeywordExpressionBlock.Init"/> on a
    /// successful match to produce the initial <see cref="IfNode"/>. At this point
    /// only the condition and body are known — the elseif/else tail is attached
    /// later in the overridden <see cref="Init"/>.
    /// </summary>
    public override SyntaxNode CreateNode(int start, int end, ExpressionNode? expr, BlockNode? body)
    {
        return new IfNode
        {
            RangeStart = start,
            RangeEnd   = end,
            Expression = expr,
            Body       = body
        };
    }

    public override Producer Init()
    {
        // Build the base if(expr){body} producer once. Calling base.Init() here
        // rather than inside the returned lambda avoids reconstructing it on every
        // parse attempt — the producer delegate is stateless and safe to reuse.
        var baseProducer = base.Init();

        // Likewise, a single ElseExpression instance is allocated once. Its Node
        // property is read after the Optional() call, so there's no risk of it
        // being overwritten by a second attempt — Optional only runs it once.
        var elseInstance = new ElseExpression();

        return (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            // Parse the mandatory `if (expr) { body }` core via the base producer.
            // If this fails there's no point looking for elseif/else — bail immediately.
            var baseMatch = baseProducer(tokens, source, pointer);
            if (!baseMatch.Success)
                return baseMatch;

            int current = baseMatch.End;

            // Inline whitespace/newline skipper — reused between each clause below.
            // Built inside the lambda so it captures the current span environment
            // correctly on each invocation.
            var whitespaceOrNewline = AnyOf(Prefab<Whitespace>(), Prefab<NewLine>());

            // Skip any formatting between the if-body closing brace and the first
            // elseif/else keyword, if present.
            var wsMatch = Optional(whitespaceOrNewline)(tokens, source, current);
            current = wsMatch.End;

            // Greedily collect elseif branches. Each iteration allocates a fresh
            // ElseIfExpression so its Node is independent of the previous one — we
            // need all of them, not just the last. The loop exits as soon as no
            // further elseif is found, leaving `current` pointing at whatever follows.
            var elseIfInstances = new List<ElseIfExpression>();
            while (true)
            {
                var instance = new ElseIfExpression();
                var match    = instance.Init()(tokens, source, current);
                if (!match.Success)
                    break;

                current = match.End;
                elseIfInstances.Add(instance);
            }

            // Skip whitespace between the last elseif (or if-body) and a potential else.
            wsMatch = Optional(whitespaceOrNewline)(tokens, source, current);
            current = wsMatch.End;

            // Attempt a single optional else branch. Optional always succeeds, so
            // `current` only advances if an else was actually present.
            var elseMatch = Optional(elseInstance.Init())(tokens, source, current);
            current = elseMatch.End;

            // Rebuild the ElseIfs list from this parse attempt. Clear() first because
            // Init() may be called multiple times on the same instance (e.g. inside
            // an Optional or AnyOf), and we don't want stale branches from a previous
            // attempt leaking into the new result.
            ElseIfs.Clear();
            foreach (var e in elseIfInstances)
            {
                // Guard against partially-matched elseif instances that produced a
                // node but without a populated ElseNode — only add fully-formed branches.
                if (e?.Node?.ElseNode is not null)
                    ElseIfs.Add(e.Node);
            }

            Else = elseInstance.Node;

            // Construct the final IfNode with all parts assembled. The condition and
            // body come from the base producer's node (cast from SyntaxNode since the
            // base property is untyped), while the elseif chain and else block are
            // the result of the tail parsing above.
            Node = new IfNode
            {
                RangeStart = baseMatch.Start,
                RangeEnd   = current,
                Expression = (base.Node as IfNode)?.Expression,
                Body       = (base.Node as IfNode)?.Body,
                ElseIfs    = ElseIfs,
                ElseNode   = Else
            };

            return new Match(true, baseMatch.Start, current);
        };
    }
}