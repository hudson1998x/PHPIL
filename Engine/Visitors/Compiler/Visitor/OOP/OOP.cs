using System.Reflection;
using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.OOP;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    private static AssemblyBuilder? _assemblyBuilder;
    private static ModuleBuilder? _moduleBuilder;

    private static ModuleBuilder ModuleBuilder
    {
        get
        {
            if (_moduleBuilder == null)
            {
                var assemblyName = new AssemblyName("PHPIL.DynamicClasses");
                _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                _moduleBuilder = _assemblyBuilder.DefineDynamicModule("MainModule");
            }
            return _moduleBuilder;
        }
    }

    public void VisitClassNode(ClassNode node, in ReadOnlySpan<char> source)
    {
        var fqn = ResolveFQN(node.Name, source);
        var typeBuilder = ModuleBuilder.DefineType(fqn.Replace("\\", "."), TypeAttributes.Public | TypeAttributes.Class);

        // Handle inheritance
        if (node.Extends != null)
        {
            var parentFqn = ResolveFQN(node.Extends, source);
            var parentType = TypeTable.GetType(parentFqn)?.RuntimeType;
            if (parentType != null)
                typeBuilder.SetParent(parentType);
        }

        // Handle interfaces
        foreach (var imp in node.Implements)
        {
            var impFqn = ResolveFQN(imp, source);
            var impType = TypeTable.GetType(impFqn)?.RuntimeType;
            if (impType != null)
                typeBuilder.AddInterfaceImplementation(impType);
        }

        // Pass 0: Handle Traits
        var traitMembers = new List<SyntaxNode>();
        foreach (var member in node.Members)
        {
            if (member is TraitUseNode tun)
            {
                foreach (var tName in tun.Traits)
                {
                    var tFqn = ResolveFQN(tName, source);
                    var tPhpType = TypeTable.GetType(tFqn);
                    if (tPhpType?.Ast is TraitNode traitAst)
                    {
                        traitMembers.AddRange(traitAst.Members);
                    }
                }
            }
        }

        // Pass 1: Define members (headers)
        var methodBuilders = new List<(MethodNode Node, MethodBuilder Builder)>();
        var fieldBuilders = new List<(PropertyNode Node, FieldBuilder Builder)>();

        var allMembers = new List<SyntaxNode>(node.Members);
        allMembers.AddRange(traitMembers);

        foreach (var member in allMembers)
        {
            if (member is MethodNode methodNode)
            {
                var methodName = methodNode.Name.Token.TextValue(in source);
                var attrs = MapMethodAttributes(methodNode.Modifiers);
                var paramTypes = new Type[methodNode.Parameters.Count];
                for (int i = 0; i < paramTypes.Length; i++) paramTypes[i] = typeof(object);
                
                var mb = typeBuilder.DefineMethod(methodName, attrs, typeof(object), paramTypes);
                methodBuilders.Add((methodNode, mb));
            }
            else if (member is PropertyNode propNode)
            {
                var propName = propNode.Name.Token.TextValue(in source).TrimStart('$');
                var attrs = MapFieldAttributes(propNode.Modifiers);
                var fb = typeBuilder.DefineField(propName, typeof(object), attrs);
                fieldBuilders.Add((propNode, fb));
            }
            else if (member is ConstantNode constNode)
            {
                var constName = constNode.Name.Token.TextValue(in source);
                var fb = typeBuilder.DefineField(constName, typeof(object), FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault);
                // Literals need to be set during definition if possible, or in static constructor
                // For PHPIL, we might just use a static field for now.
            }
        }

        // Pass 2: Compile method bodies
        foreach (var (mNode, mb) in methodBuilders)
        {
            var il = mb.GetILGenerator();
            var isStatic = mNode.Modifiers.HasFlag(PhpModifiers.Static);
            var returnType = typeof(object); // Default to mixed for now

            var innerCompiler = new Compiler(il, returnType);
            innerCompiler._currentNamespace = _currentNamespace;
            innerCompiler._currentType = typeBuilder; 
            innerCompiler._isStaticMethod = isStatic;

            foreach (var import in _useImports)
                innerCompiler._useImports[import.Key] = import.Value;

            // Load parameters into locals
            int argOffset = isStatic ? 0 : 1; 
            for (int i = 0; i < mNode.Parameters.Count; i++)
            {
            var paramName = mNode.Parameters[i].Name.Token.TextValue(in source);
                var local = innerCompiler.DeclareLocal(typeof(object));
                innerCompiler._locals[paramName] = local;

                // Emit Ldarg_i
                switch (i + argOffset)
                {
                    case 0: innerCompiler.Emit(OpCodes.Ldarg_0); break;
                    case 1: innerCompiler.Emit(OpCodes.Ldarg_1); break;
                    case 2: innerCompiler.Emit(OpCodes.Ldarg_2); break;
                    case 3: innerCompiler.Emit(OpCodes.Ldarg_3); break;
                    default: innerCompiler.Emit(OpCodes.Ldarg_S, (short)(i + argOffset)); break;
                }

                innerCompiler.Emit(OpCodes.Stloc, local);
            }

            // Generate body
            if (mNode.Body != null)
            {
                mNode.Body.Accept(innerCompiler, source);
            }

            // Implicit return null
            if (returnType != typeof(void))
            {
                innerCompiler.Emit(OpCodes.Ldnull);
            }
            innerCompiler.Emit(OpCodes.Ret);
        }

        // Pass 3: Constructor (for property defaults and __construct)
        var srcStr = source.ToString(); // Capture source before lambda since ref params can't be captured
        var phpConstruct = methodBuilders.Find(m => m.Node.Name.Token.TextValue(srcStr.AsSpan()) == "__construct");
        var ctorParamTypes = phpConstruct.Node != null ? new Type[phpConstruct.Node.Parameters.Count] : Type.EmptyTypes;
        if (phpConstruct.Node != null)
        {
            for (int i = 0; i < ctorParamTypes.Length; i++) ctorParamTypes[i] = typeof(object);
        }

        var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, ctorParamTypes);
        var ctorIl = ctor.GetILGenerator();
        
        // Base call
        ctorIl.Emit(OpCodes.Ldarg_0);
        var baseCtor = (node.Extends != null ? TypeTable.GetType(ResolveFQN(node.Extends, source))?.RuntimeType : typeof(object))?.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null) ?? typeof(object).GetConstructor(Type.EmptyTypes);
        if (baseCtor != null) ctorIl.Emit(OpCodes.Call, baseCtor);

        // Field defaults
        foreach (var (pNode, fb) in fieldBuilders)
        {
            if (pNode.DefaultValue != null)
            {
                ctorIl.Emit(OpCodes.Ldarg_0);
                var innerCompiler = new Compiler(ctorIl, typeof(void));
                innerCompiler._currentType = typeBuilder;
                pNode.DefaultValue.Accept(innerCompiler, source);
                ctorIl.Emit(OpCodes.Stfld, (System.Reflection.FieldInfo)fb);
            }
        }

        // Call __construct if exists
        if (phpConstruct.Node != null)
        {
            ctorIl.Emit(OpCodes.Ldarg_0); // 'this' for the call
            for (int i = 0; i < phpConstruct.Node.Parameters.Count; i++)
            {
                // Ldarg_1, Ldarg_2 ...
                switch (i + 1)
                {
                    case 1: ctorIl.Emit(OpCodes.Ldarg_1); break;
                    case 2: ctorIl.Emit(OpCodes.Ldarg_2); break;
                    case 3: ctorIl.Emit(OpCodes.Ldarg_3); break;
                    default: ctorIl.Emit(OpCodes.Ldarg_S, (short)(i + 1)); break;
                }
            }
            ctorIl.Emit(OpCodes.Call, phpConstruct.Builder);
            ctorIl.Emit(OpCodes.Pop); // Pop null return from __construct
        }

        ctorIl.Emit(OpCodes.Ret);

        var finishedTypeInstance = typeBuilder.CreateType();
        var phpType = TypeTable.GetType(fqn);
        if (phpType != null) phpType.RuntimeType = finishedTypeInstance;
    }

    public void VisitNewNode(NewNode node, in ReadOnlySpan<char> source)
    {
        string? fqn = null;
        if (node.ClassIdentifier is QualifiedNameNode qname)
            fqn = ResolveFQN(qname, source);
        
        if (fqn == null) throw new Exception("Could not resolve class for 'new'");

        var phpType = TypeTable.GetType(fqn);
        if (phpType?.RuntimeType == null)
            throw new Exception($"Class '{fqn}' not found.");

        var constructors = phpType.RuntimeType.GetConstructors();
        var constructor = constructors.FirstOrDefault(c => c.GetParameters().Length == node.Arguments.Count);
        
        if (constructor == null && node.Arguments.Count == 0)
            constructor = phpType.RuntimeType.GetConstructor(Type.EmptyTypes);

        if (constructor == null)
            throw new Exception($"No constructor found for '{fqn}' with {node.Arguments.Count} arguments.");

        // Emit arguments
        foreach (var arg in node.Arguments)
            arg.Accept(this, source);

        Emit(OpCodes.Newobj, constructor);
    }

    private MethodAttributes MapMethodAttributes(PhpModifiers modifiers)
    {
        MethodAttributes attrs = MethodAttributes.HideBySig;
        if (modifiers.HasFlag(PhpModifiers.Public)) attrs |= MethodAttributes.Public;
        else if (modifiers.HasFlag(PhpModifiers.Protected)) attrs |= MethodAttributes.Family;
        else if (modifiers.HasFlag(PhpModifiers.Private)) attrs |= MethodAttributes.Private;
        else attrs |= MethodAttributes.Public; // Default

        if (modifiers.HasFlag(PhpModifiers.Static)) attrs |= MethodAttributes.Static;
        if (modifiers.HasFlag(PhpModifiers.Final)) attrs |= MethodAttributes.Final;
        if (modifiers.HasFlag(PhpModifiers.Abstract)) attrs |= MethodAttributes.Abstract | MethodAttributes.Virtual;
        
        return attrs;
    }

    private FieldAttributes MapFieldAttributes(PhpModifiers modifiers)
    {
        FieldAttributes attrs = 0;
        if (modifiers.HasFlag(PhpModifiers.Public)) attrs |= FieldAttributes.Public;
        else if (modifiers.HasFlag(PhpModifiers.Protected)) attrs |= FieldAttributes.Family;
        else if (modifiers.HasFlag(PhpModifiers.Private)) attrs |= FieldAttributes.Private;
        else attrs |= FieldAttributes.Public; // Default

        if (modifiers.HasFlag(PhpModifiers.Static)) attrs |= FieldAttributes.Static;
        if (modifiers.HasFlag(PhpModifiers.Readonly)) attrs |= FieldAttributes.InitOnly;

        return attrs;
    }

    public void VisitInterfaceNode(InterfaceNode node, in ReadOnlySpan<char> source)
    {
        var fqn = ResolveFQN(node.Name, source);
        var typeBuilder = ModuleBuilder.DefineType(fqn.Replace("\\", "."), TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);

        foreach (var ext in node.Extends)
        {
            var extFqn = ResolveFQN(ext, source);
            var extType = TypeTable.GetType(extFqn)?.RuntimeType;
            if (extType != null)
                typeBuilder.AddInterfaceImplementation(extType);
        }

        var finishedType = typeBuilder.CreateType();
        var phpType = TypeTable.GetType(fqn);
        if (phpType != null) phpType.RuntimeType = finishedType;
    }

    public void VisitTraitNode(TraitNode node, in ReadOnlySpan<char> source)
    {
        // Traits are stored in the TypeTable for later application
    }

    public void VisitInstanceOfNode(InstanceOfNode node, in ReadOnlySpan<char> source)
    {
        node.Expression?.Accept(this, source);
        string? fqn = null;
        if (node.ClassIdentifier is QualifiedNameNode qname)
            fqn = ResolveFQN(qname, source);
        
        if (fqn != null)
        {
            var targetType = TypeTable.GetType(fqn)?.RuntimeType;
            if (targetType != null)
            {
                Emit(OpCodes.Isinst, targetType);
                Emit(OpCodes.Ldnull);
                Emit(OpCodes.Cgt_Un); // (isinst result) > null ? 1 : 0
                Emit(OpCodes.Box, typeof(bool));
                return;
            }
        }
        throw new NotImplementedException("instanceof for dynamic or unknown class not yet implemented.");
    }

    public void VisitObjectAccessNode(ObjectAccessNode node, in ReadOnlySpan<char> source)
    {
        node.Object?.Accept(this, source);
        var propName = node.Property.Token.TextValue(in source);

        Type? targetType = null;
        if (node.Object is VariableNode varNode && varNode.Token.TextValue(in source) == "$this")
            targetType = _currentType;

        if (targetType != null)
        {
            var field = targetType.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                Emit(OpCodes.Ldfld, field);
                return;
            }
        }
        throw new NotImplementedException($"Property access '{propName}' is not yet implemented or field not found.");
    }

    public void VisitStaticAccessNode(StaticAccessNode node, in ReadOnlySpan<char> source)
    {
        var memberName = node.MemberName.Token.TextValue(in source);
        Type? targetType = null;
        if (node.Target is QualifiedNameNode qname)
        {
            var fqn = ResolveFQN(qname, source);
            targetType = TypeTable.GetType(fqn)?.RuntimeType;
        }
        else if (node.Target is ParentNode)
        {
            if (_currentType == null) throw new Exception("'parent' used outside of class context.");
            targetType = _currentType.BaseType;
            if (targetType == null || targetType == typeof(object)) throw new Exception("Class has no parent.");
        }

        if (targetType != null)
        {
            // Try field (static property)
            var field = targetType.GetField(memberName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            if (field != null)
            {
                Emit(OpCodes.Ldsfld, field);
                return;
            }

            // Try constant (Literal field)
            if (field != null && field.IsLiteral)
            {
                Emit(OpCodes.Ldsfld, field);
                return;
            }

            // Try static method
            var method = targetType.GetMethod(memberName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            if (method != null)
            {
                 // Static calls are usually handled by VisitFunctionCallNode, but sometimes it's explicitly Class::method (rare but exists)
                 // Actually, StaticAccessNode is usually the Callee of a FunctionCallNode.
                 // If we reach here, it might be a constant access or a static property read.
            }
        }
        throw new NotImplementedException($"Static member '{memberName}' not found on {targetType?.Name}.");
    }

    public void VisitParentNode(ParentNode node, in ReadOnlySpan<char> source)
    {
        // TODO: Resolve parent class context
    }

    public void VisitMethodNode(MethodNode node, in ReadOnlySpan<char> source)
    {
        // Handled within VisitClassNode
    }

    public void VisitPropertyNode(PropertyNode node, in ReadOnlySpan<char> source)
    {
        // Handled within VisitClassNode
    }

    public void VisitTraitUseNode(TraitUseNode node, in ReadOnlySpan<char> source)
    {
        // Handled within VisitClassNode
    }

    public void VisitConstantNode(ConstantNode node, in ReadOnlySpan<char> source)
    {
        // Handled within VisitClassNode
    }

    private string ResolveFQN(SyntaxNode node, in ReadOnlySpan<char> source)
    {
        if (node is QualifiedNameNode qname)
        {
            var parts = new List<string>();
            foreach (var p in qname.Parts)
                parts.Add(p.TextValue(in source));

            string name = string.Join("\\", parts);
            if (qname.IsFullyQualified) return name;

            return string.IsNullOrEmpty(_currentNamespace) ? name : _currentNamespace + "\\" + name;
        }
        return "";
    }
}
