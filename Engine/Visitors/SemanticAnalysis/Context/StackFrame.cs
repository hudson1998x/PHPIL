using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors.SemanticAnalysis.Context
{
    [Flags]
    public enum VariableFlags : ulong
    {
        None     = 0,
        Captured = 1 << 0,
        Used     = 1 << 1,
        // future flags: Assigned = 1 << 2, Parameter = 1 << 3, etc.
    }

    public class VariableInfo
    {
        public AnalysedType Type { get; set; }
        public ulong Flags { get; set; }
        public VariableDeclaration? Node { get; set; }

        public bool IsCaptured
        {
            get => (Flags & (ulong)VariableFlags.Captured) != 0;
            set
            {
                if (value) Flags |= (ulong)VariableFlags.Captured;
                else Flags &= ~(ulong)VariableFlags.Captured;
                if (Node != null) Node.IsCaptured = value;
            }
        }

        public bool IsUsed
        {
            get => (Flags & (ulong)VariableFlags.Used) != 0;
            set
            {
                if (value) Flags |= (ulong)VariableFlags.Used;
                else Flags &= ~(ulong)VariableFlags.Used;
                if (Node != null) Node.IsUsed = value;
            }
        }

        public VariableInfo(AnalysedType type, VariableDeclaration? node)
        {
            Type = type;
            Node = node;
            Flags = 0;
        }
    }

    namespace PHPIL.Engine.Visitors.SemanticAnalysis.Context
    {
        public class StackFrame
        {
            public Dictionary<string, VariableInfo> Variables = new();
            public bool CanAscend { get; set; } = false;

            /// <summary>
            /// Declare a new variable or update an existing one.
            /// Ensures the AST node reference is stored and type is updated.
            /// </summary>
            public void DeclareOrUpdate(string name, AnalysedType type, VariableDeclaration node)
            {
                if (Variables.TryGetValue(name, out var info))
                {
                    info.Type = type;
                    info.Node ??= node; // keep existing node if already set
                }
                else
                {
                    Variables[name] = new VariableInfo(type, node);
                }
            }

            /// <summary>
            /// Mark a variable as read/used. Updates both the flag and the AST node.
            /// </summary>
            public void MarkUsed(string name)
            {
                if (Variables.TryGetValue(name, out var info))
                {
                    info.IsUsed = true;
                }
                // optional: could throw or ignore if variable not declared yet
            }

            /// <summary>
            /// Mark a variable as captured by a nested scope. Updates flag and AST node.
            /// </summary>
            public void MarkCaptured(string name)
            {
                if (Variables.TryGetValue(name, out var info))
                {
                    info.IsCaptured = true;
                }
                // optional: could throw or ignore if variable not declared yet
            }

            /// <summary>
            /// Lookup variable info by name in this frame.
            /// </summary>
            public VariableInfo? ResolveVariable(string name)
            {
                Variables.TryGetValue(name, out var info);
                return info;
            }
        }
    }
}