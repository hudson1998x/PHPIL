# PHP-IL: A CLR-Based PHP Runtime

A custom PHP runtime built on .NET that transforms PHP into MSIL and executes it on the Common Language Runtime. Heavily unit tested and continuously improving toward production-ready stability and performance.

> **Status**: Not production-ready. Large portions of the PHP standard library await implementation. The philosophy: ship something that works, improve iteratively. **Goal**: A stable PHP runtime that outperforms the official engine.

---

## Quick Start

```bash
dotnet run -- -s 0.0.0.0:8080 path/to/app/index.php
```

**How it works:**
- The server changes the execution directory to `path/to/app/`
- Executes `index.php` as if called from that directory
- Access your app at `http://localhost:8080`

**Try the sample app:**
```bash
dotnet run -- -s 0.0.0.0:8080 Samples/index.php
```
Then visit http://localhost:8080

> **Note**: On Windows and Linux, you may need to add a URL reservation. Check your OS-specific documentation.

---

## How It Works

PHP-IL transforms PHP source code through a multi-stage pipeline:

1. **Tokeniser**: Parses PHP into tokens (sub-millisecond, post-JIT warmup)
2. **AST Parser**: Builds an abstract syntax tree using grammar rules (sub-millisecond)
3. **Semantic Visitor**: Identifies types and variable usage patterns
4. **Compiler Visitor**: Generates MSIL bytecode
5. **CLR Execution**: Runs on the .NET Common Language Runtime

All stages are designed for speed and are continuously optimized.

---

## Contributing

Contributions are welcome and valuable across multiple areas:

### Areas for Contribution

| Area | Notes |
|------|-------|
| **Standard Library** | Add new functions or improve existing implementations |
| **Tokeniser** | Parser already hits sub-ms speeds: further optimizations welcome |
| **AST Parser** | Grammar-based parser running at sub-ms: improvements appreciated |
| **Semantic Visitor** | Type identification and variable tracking: execution speed improvements sought |
| **Compiler Visitor** | Open for all improvements |

##### Running tests:
```bash
dotnet run --tests
```
This runs tests in the Tests/ directory. Make sure any additional tests go in an appropriate test directory. 

### PR Guidelines

Include in every PR:
- **Files edited** with a description of changes
- **Local test results**: what you tested and the outcome
- **Clear explanation**: you can walk through what your edits do

From there:
- I'll run tests in a disposable VM
- If tests pass and the code is secure, it gets merged

### AI Submissions

AI-generated PRs are **not accepted**. All submissions must:
- Be written by you (not AI)
- Be accompanied by your explanation of the changes
- Pass all tests

---

## License

MIT License.