# Productions & Parser

## Declarative Grammar, Compact Traversal, and Ongoing Evolution

After the `Lexer` produces a compact stream of tokens, the parser layer transforms that stream into structured syntax nodes using a composable production system.

This stage is designed around:

* Declarative grammar construction
* Deterministic pointer-based traversal
* Zero-copy token access
* Minimal allocation matching
* Compact memory representation
* Strong JIT optimisation characteristics

The result is a parsing system that is both readable and highly efficient.

---

# Core Match Contract

At the foundation of the system is a minimal match descriptor:

```csharp
public readonly record struct Match(bool Success, int Start, int End);
```

A `Match` contains only:

* A success flag
* The starting token index
* The ending token index

It does not allocate syntax nodes.
It does not mutate parser state.
It simply describes a span in token space.

Because it is a `readonly record struct`:

* It remains stack-friendly
* It avoids heap allocations
* It is immutable
* It is compact

Matching stays lightweight and predictable.

---

# Unified Producer Signature

Every grammar rule shares the same signature:

```csharp
public delegate Match Producer(
    in ReadOnlySpan<Token> tokens,
    in ReadOnlySpan<char> source,
    int pointer
);
```

This design is intentional.

### ReadOnlySpan<Token>

* No copying of token arrays
* No mutation allowed
* Direct access to contiguous memory
* Strong cache locality

### ReadOnlySpan<char>

The original source buffer is passed alongside tokens so productions can:

* Extract literal values
* Build nodes without substring allocation
* Reference exact source spans

### Pointer-Based Parsing

The parser operates entirely via an integer pointer.

It does not:

* Remove tokens
* Rewrite token arrays
* Maintain complex parser state objects

It simply advances an index through a compact token span.

---

# Memory Compactness

The performance profile of the parser is rooted in the lexer’s memory model.

* `Token` is a struct
* `TokenKind` is a byte-sized enum
* Tokens are stored contiguously

This means:

* Very small per-token footprint
* Tight memory packing
* Minimal GC pressure
* Excellent CPU cache utilisation

The parser consumes this array directly via spans with zero copying.

---

# Declarative Grammar via Combinators

Productions inherit from:

```csharp
public abstract class Production
{
    public abstract Producer Init();
    public virtual void OnValue() { }
}
```

Each production describes its grammar declaratively by returning a composed `Producer`.

The system provides primitive matchers and combinators such as:

* `Token`
* `Sequence`
* `AnyOf`
* `Optional`
* `Repeated`
* `RepeatedOptional`
* `Peek`
* `Not`
* `Ref`
* `Prefab<T>`
* `AnyUntil`
* `Capture`

This allows grammar to be expressed as composable functions instead of large imperative parsing blocks.

The result is:

* Readable rule definitions
* Modular grammar components
* Clean recursive definitions
* Easy extensibility

---

# Efficient Matching Model

All matching:

* Operates directly over `ReadOnlySpan<Token>`
* Produces only small `Match` structs
* Advances pointer deterministically
* Resets cleanly on failure

No token copying occurs.
No backtracking state is stored.
Only successful matches result in syntax node allocation.

This keeps parsing:

* Linear
* Stable
* Allocation-aware

---

# Operator Precedence

Operator precedence is fully implemented.

Expression parsing respects:

* Unary precedence
* Binary operator hierarchy
* Associativity rules

This ensures expressions are parsed into correct syntax tree structures without ambiguity.

The precedence handling integrates cleanly into the production system rather than being bolted on separately.

---

# Parser Dispatch Model

The parser entry point walks the token span and dispatches based on leading `TokenKind`.

`TryProduce`:

* Selects the appropriate production
* Invokes its `Init()` producer
* Advances the pointer only on success
* Returns a constructed syntax node

This avoids:

* Reflection-based rule lookup
* Exception-driven control flow
* Dynamic grammar interpretation

Dispatch is explicit, predictable, and fast.

---

# JIT Optimisation Characteristics

Because producers are delegates with consistent signatures and operate over spans, they benefit strongly from JIT optimisation.

After warm-up:

* Small combinators are inlined
* Bounds checks are reduced
* Repetition loops stabilise
* Branch prediction improves

Combined with compact token structs, parsing becomes highly cache-friendly and stable in steady state.

---

# Current Development Status

The production system is actively evolving.

Currently implemented and stable:

* Conditionals
* Blocks
* Function declarations
* Variable assignments
* Expressions
* Return statements
* Full operator precedence handling

Loop constructs are defined structurally but not yet fully completed.

Upcoming commits will expand:

* Loop grammar finalisation
* Additional PHP constructs
* Edge case coverage
* Further grammar refinements

The combinator-based architecture makes extending the grammar straightforward. New rules can be added declaratively without altering the parsing core.

The architecture is stable.
The grammar surface continues to grow.

---

# Design Philosophy

The production system aims to combine:

* The clarity of declarative grammar
* The performance of handwritten recursive descent
* The compactness of span-based memory access
* The predictability of deterministic parsing

It is:

* Declarative but not interpreted
* Composable but not dynamic
* Recursive but controlled
* Compact and allocation-aware

As development continues, the system remains focused on readability, performance, and memory efficiency.
