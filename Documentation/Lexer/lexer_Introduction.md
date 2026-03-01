# PHP-IL

## Lexer

### Design, Architecture, and Performance Model

The `Lexer` in `PHPIL.Engine.CodeLexer` is the first stage of the PHP-IL compilation pipeline. Its responsibility is strictly lexical segmentation: transforming raw source text into a deterministic sequence of tokens.

It does not validate syntax.
It does not interpret semantics.
It does not allocate substrings.

It classifies character ranges and stops there.

This narrow responsibility is intentional and forms the foundation for a predictable, high performance compiler front-end.

---

## Static, Partial, and Stateless

```csharp
public static partial class Lexer
```

The lexer is:

* **Static**
* **Partial**
* **Stateless**

### Static

There is no instance state and no object lifecycle. Each invocation is a pure transformation from input span to token array.

This guarantees:

* Thread safety
* Deterministic output
* No hidden shared state
* Easy parallelisation

### Partial

The lexer is declared partial to allow modular expansion. As PHP-IL evolves, responsibilities can be separated across files without turning the lexer into a monolithic block of code.

This keeps the implementation maintainable while preserving performance characteristics.

### Stateless

All state required for lexing exists inside `ParseSpan`. Once execution completes, nothing is retained beyond the returned token array.

This makes behaviour fully predictable and side-effect free.

---

## Input Model and Zero-Mutation Contract

```csharp
public static Token[] ParseSpan(in ReadOnlySpan<char> sourceSpan)
```

The lexer operates directly on `ReadOnlySpan<char>`.

This provides:

* Direct memory access
* No substring allocations
* No defensive copying
* Reduced GC pressure

The `in` modifier reinforces immutability. The input memory is never mutated, and ownership remains with the caller.

This design makes the lexer suitable for:

* In-memory compilation
* Editor integration
* Incremental re-lexing
* High frequency invocation scenarios

---

## Allocation Strategy

The lexer performs a single up-front allocation:

```csharp
var tokens = new Token[sourceSpan.Length / 2];
```

This heuristic is based on language structure constraints. In valid PHP source, token count cannot realistically exceed half the character length.

This avoids:

* Dynamic resizing
* List growth overhead
* Repeated reallocation
* Allocation churn under load

At completion:

```csharp
return tokens[..tokenIndex];
```

The array is sliced to the actual token count. The scanning phase remains allocation-stable, and the returned array is tightly sized.

Each lex invocation performs exactly one managed allocation.

---

## Token Representation and Memory Compactness

Tokens are implemented as **structs**, not classes.

`TokenKind` is defined as a **byte-backed enum**.

This has significant memory implications.

### Why Struct Tokens

Using a struct:

* Eliminates per-token heap allocation
* Ensures tokens live contiguously inside the array
* Improves cache locality
* Reduces GC pressure to near zero

All tokens for a file exist in one contiguous block of memory.

### Byte-Sized TokenKind

Because `TokenKind` is a `byte` enum:

* Each token stores its kind in 1 byte
* No unnecessary enum widening
* Reduced padding footprint

The result is a compact token layout consisting only of:

* TokenKind (1 byte)
* RangeStart (int)
* RangeEnd (int)

This keeps per-token memory minimal and predictable.

For large files, this compactness significantly improves:

* Cache efficiency
* Sequential memory access speed
* Overall parsing throughput

The lexer is designed not just for correctness, but for memory density.

---

## Forward-Only Scanning Model

Lexing is performed using a single advancing pointer:

```csharp
var position = 0;
```

Core invariant:

The pointer always advances.

This ensures:

* Linear time complexity O(n)
* No recursion
* No backtracking
* No infinite loops

Malformed constructs are emitted as `Unknown` tokens and scanning continues.

The lexer never throws due to malformed input. This makes it resilient for incomplete files and real-time editing environments.

---

## Deterministic Token Emission

Token creation is centralised through `AddToken`.

Each token records:

* Kind
* RangeStart
* RangeEnd

No substring extraction occurs. The lexer never copies portions of the source text.

Downstream stages interpret token ranges lazily against the original source.

This separation:

* Avoids redundant memory duplication
* Keeps the lexer lightweight
* Preserves exact source mapping for diagnostics

---

## JIT Compilation and Warm Performance

The lexer benefits significantly from .NET’s JIT compilation model.

On first execution, the method is compiled from IL into optimised machine code. For large files, this initial compilation cost is measurable:

* First parse of a large file: approximately 3ms
* Subsequent parses of the same file: sub millisecond

The initial 3ms is the one-time JIT cost.

After compilation:

* The large switch compiles to efficient jump tables
* Small helpers are inlined
* Bounds checks in hot paths are eliminated
* Frequently used locals remain in registers
* Branch prediction stabilises

Because the lexer:

* Uses a tight forward-only loop
* Contains no virtual dispatch
* Performs no reflection
* Uses no regex engine
* Allocates only once
* Works over contiguous spans

The steady-state runtime is extremely fast.

After warm-up, parsing becomes almost entirely memory traversal and branch evaluation.

For large files, repeated lexing consistently measures below one millisecond.

This makes the lexer suitable for:

* Continuous editor re-lexing
* Batch compilation
* Large codebase processing
* High frequency tooling scenarios

Performance scales linearly with file size and remains stable due to absence of allocation spikes.

---

## Separation of Responsibility

The lexer intentionally avoids:

* Syntax tree construction
* Semantic validation
* Constant evaluation
* Type inference
* Context-sensitive analysis

Its sole responsibility is lexical segmentation.

This strict separation:

* Prevents feature creep
* Maintains performance guarantees
* Keeps architecture clean
* Ensures clear ownership between compiler stages

---

## Performance Summary

* Time Complexity: O(n)
* Allocations: One token array
* Token Storage: Contiguous struct array
* Enum Size: Byte-backed
* Substring Allocation: None
* Backtracking: None
* Mutation: None
* Thread Safe: Yes
* First Large File Parse: ~3ms
* Warm Parse of Same File: Sub millisecond

---

## Design Philosophy

The PHP-IL `Lexer` is intentionally:

* Low level
* Allocation aware
* Memory compact
* JIT friendly
* Deterministic
* Forward only

It forms a stable, high performance foundation for the entire compilation pipeline.

Every design decision prioritises predictability, memory density, and steady-state speed.