# JSON Serialization of Syntax Nodes

Each `SyntaxNode` is **JSON serializable** via a `ToJson` method. The default implementation produces a simple JSON object identifying the node type.

```csharp
using System.Text;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class SyntaxNode
{
    public virtual void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append('{');
        builder.Append($"\"type\": \"{GetType().Name}\"");
        builder.Append('}');
    }
}
```

### How it works

* `StringBuilder` is used to **avoid unnecessary allocations**
* `ReadOnlySpan<char>` and `ReadOnlySpan<Token>` provide **allocation-free access** to the source and tokens
* The method is **partial**, allowing each derived node type to extend it with additional properties or nested children

---

### Usage Example

```csharp
var builder = new StringBuilder();
rootNode.ToJson(sourceSpan, tokenSpan, builder);
Console.WriteLine(builder.ToString());
```

This produces a lightweight, hierarchical JSON representation of the AST. More complex nodes can override `ToJson` to include:

* Child nodes
* Token values
* Source ranges

This makes the AST **tool-friendly** for visualization, debugging, or exporting to IDE tooling.
