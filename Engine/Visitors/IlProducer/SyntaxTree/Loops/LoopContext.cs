using System.Reflection.Emit;

namespace PHPIL.Engine.Visitors.IlProducer;

/// <summary>
/// Represents the control flow targets for a specific loop nesting level.
/// </summary>
/// <param name="BreakLabel">The IL label marking the exit point immediately following the loop.</param>
/// <param name="ContinueLabel">The IL label marking the re-evaluation or increment point of the loop.</param>
/// <param name="Name">An optional identifier for labeled break/continue support.</param>
public record LoopContext(Label BreakLabel, Label ContinueLabel, string? Name = null);