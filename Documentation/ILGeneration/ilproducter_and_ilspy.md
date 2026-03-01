# IlProducer

The **`IlProducer`** class is the core component responsible for **generating IL from the syntax tree**. It implements `IVisitor`, so every `SyntaxNode` can call `Accept` with a specific visitor, allowing for **custom IL emission logic per node type**.

### Key Components

* **RuntimeContext**
  Tracks the current stack frames, local variables, and scope. `EnterScope` and `ExitScope` push/pop frames to manage variable lifetimes correctly.

* **DynamicMethod & ILGenerator**
  `IlProducer` can either create a new `DynamicMethod` for execution (`phpil_main`) or accept an existing `ILGenerator`. This allows both **dynamic runtime execution** and **integration with larger IL generation pipelines**.

* **ILSpy**
  A logging wrapper around the `ILGenerator` that outputs **all emitted IL** when enabled. Every opcode, local, method call, and branch is logged, making it easy to debug emitted IL if something goes wrong.

* **LastEmittedType**
  Tracks the type of the last value emitted, useful for type checking or knowing what the IL stack currently contains.

---

### Example Workflow

1. A `SyntaxNode` calls its `Accept` method, passing the `IlProducer` instance.
2. `IlProducer` provides the `ILSpy` instance to the node.
3. The node decides which IL to emit and calls `Emit` via `ILSpy`.
4. `ILSpy` optionally logs the emitted instructions for debugging.
5. The `RuntimeContext` is updated if the node creates locals, enters a scope, or performs other state changes.

---

## Syntax Nodes Driving IL

Each syntax node knows **what IL to emit for which visitor**. For example, a `VariableNode`:

```csharp
public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
{
    if (visitor is not IlProducer ilProducer) return;
    var il = ilProducer.GetILGenerator();

    // Load local variable (already PhpValue!)
    if (ilProducer.GetContext().TryGetVariableSlot(Token.TextValue(in source), out var localIndex))
    {
        il.Emit(OpCodes.Ldloc, localIndex);   
    }
    else
    {
        throw new Exception("Variable not found");
    }

    ilProducer.LastEmittedType = typeof(PHPIL.Engine.Runtime.Types.PhpValue);
}
```

Here:

* The node **asks the runtime context** for its variable slot.
* It emits the correct `OpCodes.Ldloc` instruction via `ILSpy`.
* It updates `LastEmittedType` to keep type information consistent.

This pattern is **replicated across all node types**, so each node encapsulates **exactly what IL it produces** for `IlProducer`. This makes the system:

* Highly modular
* Easy to extend with new node types
* Debug-friendly, especially with `ILSpy` logging

---

### Benefits of this Design

* **Separation of Concerns:** Syntax nodes define *what* to emit; `IlProducer` handles *how* to emit.
* **Debugging:** `ILSpy` provides a full log of IL emission, which is invaluable for tracing execution issues.
* **Runtime Execution:** Nodes emit directly into a `DynamicMethod`, which can then be invoked immediately for fast execution.
* **Extensibility:** New nodes and IL emission logic can be added without changing `IlProducer`.

---

In short, `IlProducer` plus `ILSpy` creates a **fast, traceable IL generation pipeline**, while syntax nodes remain **self-contained and responsible for their emitted instructions**, maintaining both clarity and efficiency.