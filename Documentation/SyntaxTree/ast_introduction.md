# Abstract Syntax Tree

## Pratt-Based Expressions and Visitor-Driven IL Emission

After productions match token sequences, they construct an **Abstract Syntax Tree**.

The AST represents the semantic structure of the program in a hierarchical and strongly-typed form. It is designed to be:

* Deterministic
* Structured
* Easy to traverse
* Ready for code generation
* Extendable without modifying the parser

The parser’s job ends once the tree is constructed. Execution logic lives elsewhere.

---

# Pratt Parser for Expressions

Expressions are parsed using a **Pratt parser model**.

This provides:

* Accurate operator precedence
* Proper associativity handling
* Clean prefix and postfix support
* Minimal grammar complexity

Each expression node participates via:

* **Nud** for prefix and primary expressions
* **Led** for infix and postfix behaviour

Because binding power is encoded directly into expression handling, operator precedence is handled correctly without large precedence tables or deeply nested recursive descent chains.

This ensures:

* Correct binary precedence hierarchy
* Proper unary binding
* Accurate postfix chaining
* Predictable expression nesting

Postfix operators bind tighter than infix operators, so chained expressions and increments are parsed into the correct tree shape.

The Pratt model integrates cleanly with the production system rather than existing as a separate parsing layer.

---

# AST Node Structure

Each syntactic construct maps to a dedicated `SyntaxNode` type.

Examples include:

* Block nodes
* Expression nodes
* Assignment nodes
* Function declaration nodes
* Conditional nodes
* Return nodes

Nodes represent semantics, not tokens. They preserve structural meaning rather than raw lexical form.

Only successful matches produce nodes, which avoids speculative allocation and rollback complexity.

---

# Visitor-Based Execution Model

The AST uses a visitor-based execution model for IL generation.

Each node exposes an `Accept` method.
This method is implemented using partial definitions, allowing behaviour to be extended cleanly without bloating the core node definitions.

Nodes extend an `IVisitor` contract that defines default behaviours. Individual visitors can then provide specific logic while inheriting standard traversal structure.

This provides:

* Clean separation between structure and behaviour
* No code generation logic embedded directly in nodes
* The ability to plug in different visitors later
* A consistent traversal pattern

The AST itself remains focused purely on structure.

---

# Controlled Extensibility

The combination of:

* Production-based statement parsing
* Pratt-based expression parsing
* Visitor-driven execution

creates a layered architecture where:

* Grammar can expand without changing execution logic
* Execution logic can evolve without modifying parsing
* New visitors can be added without altering AST structure

Each layer remains independent and predictable.

---

# Memory and Performance Alignment

The AST sits on top of:

* Struct-based tokens
* Byte-sized `TokenKind`
* Span-based traversal
* Allocation-light matching

Only valid constructs result in node allocation.

There is no speculative tree building.
There is no rollback allocation churn.

The system remains allocation-aware from lexing through to IL generation.

---

# Current State

The AST architecture is stable and integrated with:

* Fully working operator precedence
* Accurate Nud and Led handling
* Visitor-driven IL generation
* Partial-based `Accept` extensibility

Loop constructs are still under development, but expression handling and structural nodes are solid.

The core architecture is complete.
The grammar surface continues to expand.