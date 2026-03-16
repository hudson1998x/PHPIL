using System.Reflection;
using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.OOP;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Lazily-initialised <see cref="AssemblyBuilder"/> that hosts all dynamically defined PHP classes.
    /// </summary>
    private static AssemblyBuilder? _assemblyBuilder;

    /// <summary>
    /// Lazily-initialised <see cref="ModuleBuilder"/> within <see cref="_assemblyBuilder"/> into
    /// which all PHP class types are emitted.
    /// </summary>
    private static ModuleBuilder? _moduleBuilder;

    /// <summary>
    /// Gets the shared <see cref="ModuleBuilder"/> used to define dynamic PHP class types,
    /// initialising the underlying dynamic assembly on first access.
    /// </summary>
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

    /// <summary>
    /// Resets the shared dynamic assembly and module, discarding all previously emitted types.
    /// </summary>
    /// <remarks>
    /// Intended for use between test runs or interpreter resets where a fresh type namespace is required.
    /// </remarks>
    public static void ResetModule()
    {
        _assemblyBuilder = null;
        _moduleBuilder = null;
    }

    /// <summary>
    /// Emits a dynamic CLR type for a PHP class declaration, including inheritance, interface
    /// implementation, trait composition, fields, constants, methods, and a parameterless constructor.
    /// </summary>
    /// <param name="node">The <see cref="ClassNode"/> representing the PHP class declaration.</param>
    /// <param name="source">The original source text, used to resolve names, modifiers, and literal values.</param>
    /// <remarks>
    /// <para>
    /// Compilation proceeds in four passes:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       <b>Pass 0 — Trait composition:</b> any <see cref="TraitUseNode"/> members are resolved
    ///       via <c>TypeTable</c> and their AST members are appended to the working member list.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Pass 1 — Member headers:</b> <see cref="MethodNode"/> entries are declared via
    ///       <c>TypeBuilder.DefineMethod</c> with attributes mapped by <see cref="MapMethodAttributes"/>;
    ///       <see cref="PropertyNode"/> fields are declared via <c>DefineField</c> with attributes
    ///       mapped by <see cref="MapFieldAttributes"/> and registered in <c>PhpType.FieldBuilders</c>;
    ///       <see cref="ConstantNode"/> entries are declared as <c>Literal</c> static fields with their
    ///       values set immediately for <c>int</c> and <c>string</c> literals.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Pass 2 — Method bodies:</b> a child <see cref="Compiler"/> is created per method,
    ///       inheriting the current namespace, use-imports, and <c>_currentType</c>. Parameters are
    ///       loaded from arguments into typed locals. For instance methods, <c>$this</c> is bound to
    ///       <c>Ldarg_0</c>. The method body is emitted, followed by an implicit <see langword="null"/>
    ///       return for non-<see langword="void"/> methods.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Pass 3 — Constructor:</b> a public parameterless constructor is synthesised. It chains
    ///       to the base constructor, then emits field default initialisers for any
    ///       <see cref="PropertyNode"/> with a <c>DefaultValue</c>. PHP <c>__construct</c> is
    ///       intentionally not invoked here — it is called as a regular method after object creation.
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// After <c>TypeBuilder.CreateType</c> completes, static property defaults are written via
    /// <c>RuntimeHelpers.SetStaticPropertyDefault</c> for literal and array-literal initialisers,
    /// and all methods are registered with <c>TypeTable</c> for later lookup.
    /// </para>
    /// </remarks>
    public void VisitClassNode(ClassNode node, in ReadOnlySpan<char> source)
    {
        var fqn = ResolveFQN(node.Name, source);
        var typeName = fqn.Replace("\\", ".");
        var typeBuilder = ModuleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);

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
        
        // Get PhpType for field lookup
        var phpType = TypeTable.GetType(fqn);

        var allMembers = new List<SyntaxNode>(node.Members);
        allMembers.AddRange(traitMembers);

        foreach (var member in allMembers)
        {
            if (member is MethodNode methodNode)
            {
                var methodName = methodNode.Name.Token.TextValue(in source);
                var attrs = MapMethodAttributes(methodNode.Modifiers);
                var paramTypes = new Type[methodNode.Parameters.Count];
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    if (methodNode.Parameters[i].IsVariadic)
                        paramTypes[i] = typeof(object[]);
                    else
                        paramTypes[i] = typeof(object);
                }
                
                var mb = typeBuilder.DefineMethod(methodName, attrs, typeof(object), paramTypes);
                methodBuilders.Add((methodNode, mb));
            }
            else if (member is PropertyNode propNode)
            {
                var propName = propNode.Name.Token.TextValue(in source).TrimStart('$');
                var attrs = MapFieldAttributes(propNode.Modifiers);
                var fb = typeBuilder.DefineField(propName, typeof(object), attrs);
                fieldBuilders.Add((propNode, fb));
                phpType.FieldBuilders[propName] = fb;
            }
            else if (member is ConstantNode constNode)
            {
                var constName = constNode.Name.Token.TextValue(in source);
                var fb = typeBuilder.DefineField(constName, typeof(object), FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault);
                
                // Set the constant value if it's a literal
                if (constNode.Value is LiteralNode literal)
                {
                    var tokenText = literal.Token.TextValue(in source);
                    if (literal.Token.Kind == TokenKind.IntLiteral)
                    {
                        if (int.TryParse(tokenText, out int intVal))
                            fb.SetConstant(intVal);
                    }
                    else if (literal.Token.Kind == TokenKind.StringLiteral)
                    {
                        fb.SetConstant(tokenText.Trim('"', '\''));
                    }
                }
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
            innerCompiler._currentMethodName = mNode.Name.Token.TextValue(in source); // Track method name for __METHOD__

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

            // Declare $this for instance methods
            if (!isStatic)
            {
                var thisLocal = innerCompiler.DeclareLocal(typeof(object));
                innerCompiler._locals["$this"] = thisLocal;
                innerCompiler.Emit(OpCodes.Ldarg_0);
                innerCompiler.Emit(OpCodes.Stloc, thisLocal);
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

        // Pass 3: Constructor (for property defaults)
        // Always create a parameterless constructor - __construct is called separately after object creation
        var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
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
                ctorIl.Emit(OpCodes.Box, typeof(int));
                ctorIl.Emit(OpCodes.Stfld, (System.Reflection.FieldInfo)fb);
            }
        }

        // Note: We don't call __construct here - it's handled as a regular method call after object creation
        // For PHP-style constructors, the user should call __construct() explicitly if needed

        ctorIl.Emit(OpCodes.Ret);

        var finishedTypeInstance = typeBuilder.CreateType();
        if (phpType != null) phpType.RuntimeType = finishedTypeInstance;
        
        // Set static property defaults in the runtime helper dictionary
        foreach (var (pNode, fb) in fieldBuilders)
        {
            if (pNode.Modifiers.HasFlag(PhpModifiers.Static) && pNode.DefaultValue != null)
            {
                // Try to evaluate the default value if it's a literal
                object? defaultValue = null;
                if (pNode.DefaultValue is LiteralNode literal)
                {
                    var tokenText = literal.Token.TextValue(in source);
                    if (literal.Token.Kind == TokenKind.IntLiteral)
                    {
                        if (int.TryParse(tokenText, out int intVal))
                            defaultValue = intVal;
                    }
                    else if (literal.Token.Kind == TokenKind.StringLiteral)
                    {
                        defaultValue = tokenText.Trim('"', '\'');
                    }
                }
                else if (pNode.DefaultValue is ArrayLiteralNode)
                {
                    defaultValue = new Dictionary<object, object?>();
                }
                
                if (defaultValue != null)
                {
                    PHPIL.Engine.Runtime.Sdk.RuntimeHelpers.SetStaticPropertyDefault(fqn, fb.Name, defaultValue);
                }
            }
        }
        
        // Register methods for later lookup
        foreach (var (mNode, builder) in methodBuilders)
        {
            var methodName = mNode.Name.Token.TextValue(in source);
            var resolvedName = string.IsNullOrEmpty(_currentNamespace) ? methodName : _currentNamespace + "\\" + methodName;
            TypeTable.RegisterMethod(fqn, resolvedName, builder);
        }
    }

    /// <summary>
    /// Emits IL to instantiate a PHP class and invoke its <c>__construct</c> method if present.
    /// </summary>
    /// <param name="node">The <see cref="NewNode"/> representing the <c>new ClassName(...)</c> expression.</param>
    /// <param name="source">The original source text, used to resolve the class identifier.</param>
    /// <exception cref="Exception">
    /// Thrown when the class identifier cannot be resolved, or when the resolved type has no
    /// parameterless constructor.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If the class's <c>RuntimeType</c> is not yet available in <c>TypeTable</c> (e.g. for
    /// forward-referenced or lazily-loaded classes), <c>RuntimeHelpers.ResolveAndCreate</c> is
    /// called at runtime to instantiate the object by name, followed by
    /// <c>RuntimeHelpers.TryCallConstruct</c> with the packed argument array.
    /// </para>
    /// <para>
    /// When the type is known at compile time, <see cref="OpCodes.Newobj"/> is emitted directly
    /// against the parameterless constructor. The new object is duplicated on the stack so that
    /// it remains as the expression result after <c>TryCallConstruct</c> consumes its copy to
    /// invoke <c>__construct</c>.
    /// </para>
    /// </remarks>
    public void VisitNewNode(NewNode node, in ReadOnlySpan<char> source)
    {
        string? fqn = null;
        if (node.ClassIdentifier is QualifiedNameNode qname)
            fqn = ResolveFQN(qname, source);
        
        if (fqn == null) throw new Exception("Could not resolve class for 'new'");

        var phpType = TypeTable.GetType(fqn);
        
        var tryCallConstructMethod = typeof(PHPIL.Engine.Runtime.Sdk.RuntimeHelpers).GetMethod("TryCallConstruct")!;

        if (phpType?.RuntimeType == null)
        {
            Emit(OpCodes.Ldstr, fqn);
            var resolveAndCreateMethod = typeof(PHPIL.Engine.Runtime.Sdk.RuntimeHelpers).GetMethod("ResolveAndCreate")!;
            Emit(OpCodes.Call, resolveAndCreateMethod);
            
            Emit(OpCodes.Dup);
            Emit(OpCodes.Ldc_I4, node.Arguments.Count);
            Emit(OpCodes.Newarr, typeof(object));
            
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                Emit(OpCodes.Dup);
                Emit(OpCodes.Ldc_I4, i);
                node.Arguments[i].Accept(this, source);
                EmitBoxingIfLiteral(node.Arguments[i]);
                Emit(OpCodes.Stelem_Ref);
            }
            
            Emit(OpCodes.Call, tryCallConstructMethod);
            return;
        }

        var constructorInfo = phpType.RuntimeType.GetConstructor(Type.EmptyTypes);
        if (constructorInfo == null)
            throw new Exception($"No parameterless constructor found for '{fqn}'.");

        Emit(OpCodes.Newobj, constructorInfo);
        
        // Duplicate object so it remains on stack after TryCallConstruct
        Emit(OpCodes.Dup);
        
        Emit(OpCodes.Ldc_I4, node.Arguments.Count);
        Emit(OpCodes.Newarr, typeof(object));
        
        for (int i = 0; i < node.Arguments.Count; i++)
        {
            Emit(OpCodes.Dup);
            Emit(OpCodes.Ldc_I4, i);
            node.Arguments[i].Accept(this, source);

            EmitBoxingIfLiteral(node.Arguments[i]);
            Emit(OpCodes.Stelem_Ref);
        }
        
        Emit(OpCodes.Call, tryCallConstructMethod);
    }

    /// <summary>
    /// Maps PHP method modifiers to their corresponding <see cref="MethodAttributes"/> flags.
    /// </summary>
    /// <param name="modifiers">The <see cref="PhpModifiers"/> declared on the method.</param>
    /// <returns>
    /// A <see cref="MethodAttributes"/> value combining visibility, static, final, and abstract
    /// flags. Defaults to <see cref="MethodAttributes.Public"/> when no visibility modifier is present.
    /// </returns>
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

    /// <summary>
    /// Maps PHP property modifiers to their corresponding <see cref="FieldAttributes"/> flags.
    /// </summary>
    /// <param name="modifiers">The <see cref="PhpModifiers"/> declared on the property.</param>
    /// <returns>
    /// A <see cref="FieldAttributes"/> value combining visibility, static, and readonly flags.
    /// Defaults to <see cref="FieldAttributes.Public"/> when no visibility modifier is present.
    /// </returns>
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

    /// <summary>
    /// Emits a dynamic CLR interface type for a PHP interface declaration and registers it
    /// with <c>TypeTable</c>.
    /// </summary>
    /// <param name="node">The <see cref="InterfaceNode"/> representing the PHP interface declaration.</param>
    /// <param name="source">The original source text, used to resolve the interface and parent interface names.</param>
    /// <remarks>
    /// Extended interfaces are resolved via <c>TypeTable</c> and applied via
    /// <c>AddInterfaceImplementation</c>. No method stubs are emitted — interface method
    /// signatures are enforced at the PHP semantic level rather than in the CLR type.
    /// </remarks>
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

    /// <summary>
    /// Visitor stub for a PHP trait declaration.
    /// </summary>
    /// <param name="node">The <see cref="TraitNode"/> representing the trait declaration.</param>
    /// <param name="source">The original source text.</param>
    /// <remarks>
    /// Traits are stored in <c>TypeTable</c> by the semantic analysis pass and applied to
    /// consuming classes in <see cref="VisitClassNode"/> (Pass 0). No IL is emitted here.
    /// </remarks>
    public void VisitTraitNode(TraitNode node, in ReadOnlySpan<char> source)
    {
        // Traits are stored in the TypeTable for later application
    }

    /// <summary>
    /// Emits IL for an <c>instanceof</c> expression, leaving a boxed <see cref="bool"/> on the stack.
    /// </summary>
    /// <param name="node">The <see cref="InstanceOfNode"/> representing the instanceof check.</param>
    /// <param name="source">The original source text, used to resolve the target class identifier.</param>
    /// <exception cref="NotImplementedException">
    /// Thrown when the class identifier cannot be resolved and is not a <see cref="VariableNode"/>.
    /// </exception>
    /// <remarks>
    /// When the target type is known at compile time, <see cref="OpCodes.Isinst"/> is emitted
    /// against the resolved CLR type, and the result is compared against <see langword="null"/>
    /// via <see cref="OpCodes.Cgt_Un"/> to produce a <see cref="bool"/>. When the identifier is
    /// a variable (i.e. a dynamic class name), <c>RuntimeHelpers.InstanceOf</c> is called instead.
    /// </remarks>
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
                Emit(OpCodes.Cgt_Un);
                Emit(OpCodes.Box, typeof(bool));
                return;
            }
        }
        
        // Handle variable case: $obj instanceof $className
        if (node.ClassIdentifier is VariableNode varNode)
        {
            node.ClassIdentifier.Accept(this, source);
            var instanceOfMethod = typeof(PHPIL.Engine.Runtime.Sdk.RuntimeHelpers).GetMethod("InstanceOf")!;
            Emit(OpCodes.Call, instanceOfMethod);
            Emit(OpCodes.Box, typeof(bool));
            return;
        }
        
        throw new NotImplementedException("instanceof for dynamic or unknown class not yet implemented.");
    }

    /// <summary>
    /// Emits IL to load a property value from an object access expression (<c>$obj->property</c>).
    /// </summary>
    /// <param name="node">The <see cref="ObjectAccessNode"/> representing the property access.</param>
    /// <param name="source">The original source text, used to resolve the property name.</param>
    /// <remarks>
    /// For <c>$this->property</c> accesses within an instance method, the property is looked up
    /// in <c>PhpType.FieldBuilders</c> and loaded via a direct <see cref="OpCodes.Ldfld"/>
    /// instruction, bypassing reflection for efficiency. If the field is not found in
    /// <c>FieldBuilders</c>, or the object is not <c>$this</c>, <c>RuntimeHelpers.GetProperty</c>
    /// is used for dynamic property resolution.
    /// </remarks>
    public void VisitObjectAccessNode(ObjectAccessNode node, in ReadOnlySpan<char> source)
    {
        node.Object?.Accept(this, source);
        var propName = node.Property.Token.TextValue(in source);

        // Check if it's $this and we have the field builder
        if (node.Object is VariableNode varNode && varNode.Token.TextValue(in source) == "$this" && !_isStaticMethod && _currentType != null)
        {
            // Get the PhpType for the current class
            var fqn = _currentType.Name.Replace(".", "\\");
            var phpType = TypeTable.GetType(fqn);
            if (phpType != null && phpType.FieldBuilders.TryGetValue(propName, out var fieldBuilder))
            {
                // Direct field access - much more efficient than reflection
                Emit(OpCodes.Ldfld, fieldBuilder);
                return;
            }
            
            // Fall back to runtime helper if field not found
            Emit(OpCodes.Ldstr, propName);
            var getPropMethod = typeof(PHPIL.Engine.Runtime.Sdk.RuntimeHelpers).GetMethod("GetProperty", new[] { typeof(object), typeof(string) })!;
            Emit(OpCodes.Call, getPropMethod);
            return;
        }

        // For non-$this, use runtime helper
        Emit(OpCodes.Ldstr, propName);
        var getPropMethod2 = typeof(PHPIL.Engine.Runtime.Sdk.RuntimeHelpers).GetMethod("GetProperty", new[] { typeof(object), typeof(string) })!;
        Emit(OpCodes.Call, getPropMethod2);
    }

    /// <summary>
    /// Emits IL to load a static property or class constant via a static access expression
    /// (<c>ClassName::$property</c> or <c>ClassName::CONSTANT</c>).
    /// </summary>
    /// <param name="node">The <see cref="StaticAccessNode"/> representing the static access.</param>
    /// <param name="source">The original source text, used to resolve the target type and member name.</param>
    /// <exception cref="Exception">
    /// Thrown when the member name cannot be resolved, when <c>self</c> is used outside a class
    /// context, or when <c>parent</c> is used in a class with no base type.
    /// </exception>
    /// <exception cref="NotImplementedException">
    /// Thrown when the target is a dynamic expression that cannot be resolved at compile time.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Resolution is attempted in the following order:
    /// </para>
    /// <list type="number">
    ///   <item><description><c>self</c> is aliased to <c>_currentType</c>; <c>parent</c> resolves to <c>_currentType.BaseType</c>.</description></item>
    ///   <item><description>If the member is found in <c>PhpType.FieldBuilders</c> or as a static field on the runtime type, <c>RuntimeHelpers.GetStaticProperty</c> is called.</description></item>
    ///   <item><description>If the member is a <c>Literal</c> constant field, its value is pushed directly as an <see cref="int"/> or <see cref="string"/> constant.</description></item>
    ///   <item><description>If the type is not yet fully created (e.g. during circular class compilation), <see langword="null"/> is pushed as a fallback.</description></item>
    /// </list>
    /// </remarks>
    public void VisitStaticAccessNode(StaticAccessNode node, in ReadOnlySpan<char> source)
    {
        string? memberName = null;
        if (node.MemberName is IdentifierNode idNode)
            memberName = idNode.Token.TextValue(in source);
        else if (node.MemberName is VariableNode varNode)
            memberName = varNode.Token.TextValue(in source).TrimStart('$');
            
        if (memberName == null)
            throw new Exception("Cannot resolve static member name");
            
        Type? targetType = null;
        string? fqn = null;
        if (node.Target is QualifiedNameNode qname)
        {
            fqn = ResolveFQN(qname, source);
            // Special handling for "self" - resolve to current class
            if (fqn.Equals("self", StringComparison.OrdinalIgnoreCase))
            {
                if (_currentType == null)
                    throw new Exception("'self' used outside of class context.");
                targetType = _currentType;
                // Get the actual FQN for runtime helper lookup - build from namespace and type name
                var className = targetType.Name;
                fqn = string.IsNullOrEmpty(_currentNamespace) ? className : _currentNamespace + "\\" + className;
            }
            else
            {
                targetType = TypeTable.GetType(fqn)?.RuntimeType;
            }
        }
        else if (node.Target is ParentNode)
        {
            if (_currentType == null) throw new Exception("'parent' used outside of class context.");
            targetType = _currentType.BaseType;
            if (targetType == null || targetType == typeof(object)) throw new Exception("Class has no parent.");
        }

        if (targetType != null)
        {
            // Try to get the PhpType and check FieldBuilders first
            var phpType = TypeTable.GetType(fqn ?? targetType.Name.Replace(".", "\\"));
            if (phpType != null && phpType.FieldBuilders.TryGetValue(memberName, out var fieldBuilder))
            {
                // Use runtime helper to get the static property value
                Emit(OpCodes.Ldstr, memberName);
                Emit(OpCodes.Ldstr, fqn?.Replace("\\", ".") ?? targetType.Name);
                var getStaticProp = typeof(PHPIL.Engine.Runtime.Sdk.RuntimeHelpers).GetMethod("GetStaticProperty")!;
                Emit(OpCodes.Call, getStaticProp);
                return;
            }
            
            // Try field (static property) via reflection on RuntimeType
            try
            {
                var runtimeType = phpType?.RuntimeType ?? targetType;
                var field = runtimeType.GetField(memberName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                if (field != null)
                {
                    Emit(OpCodes.Ldstr, memberName);
                    Emit(OpCodes.Ldstr, fqn?.Replace("\\", ".") ?? targetType.Name);
                    var getStaticProp = typeof(PHPIL.Engine.Runtime.Sdk.RuntimeHelpers).GetMethod("GetStaticProperty")!;
                    Emit(OpCodes.Call, getStaticProp);
                    return;
                }
            }
            catch (NotSupportedException)
            {
                // Type not fully created yet - use runtime helper
            }

            // Try constant
            try
            {
                var constField = targetType.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
                if (constField != null && constField.IsLiteral)
                {
                    var value = constField.GetValue(null);
                    if (value is int intVal)
                        Emit(OpCodes.Ldc_I4, intVal);
                    else if (value is string strVal)
                        Emit(OpCodes.Ldstr, strVal);
                    else
                        Emit(OpCodes.Ldnull);
                    return;
                }
            }
            catch (NotSupportedException)
            {
                // Type not fully created yet - skip
            }
            
            // Try to handle dynamic module case - emit null for now
            Emit(OpCodes.Ldnull);
            return;
        }
        
        Emit(OpCodes.Ldnull);
    }

    /// <summary>
    /// Visitor stub for the <c>parent</c> keyword used as a standalone expression.
    /// </summary>
    /// <param name="node">The <see cref="ParentNode"/>.</param>
    /// <param name="source">The original source text.</param>
    /// <remarks>
    /// Parent resolution in static access and method call contexts is handled inline by
    /// <see cref="VisitStaticAccessNode"/> and <see cref="VisitFunctionCallNode"/> respectively.
    /// </remarks>
    public void VisitParentNode(ParentNode node, in ReadOnlySpan<char> source)
    {
        // TODO: Resolve parent class context
    }

    /// <summary>
    /// Visitor stub for a method declaration node.
    /// </summary>
    /// <param name="node">The <see cref="MethodNode"/> representing the method declaration.</param>
    /// <param name="source">The original source text.</param>
    /// <remarks>
    /// Method compilation is handled entirely within <see cref="VisitClassNode"/> (Pass 2).
    /// </remarks>
    public void VisitMethodNode(MethodNode node, in ReadOnlySpan<char> source)
    {
        // Handled within VisitClassNode
    }

    /// <summary>
    /// Visitor stub for a property declaration node.
    /// </summary>
    /// <param name="node">The <see cref="PropertyNode"/> representing the property declaration.</param>
    /// <param name="source">The original source text.</param>
    /// <remarks>
    /// Property field definition and default value initialisation are handled entirely within
    /// <see cref="VisitClassNode"/> (Passes 1 and 3).
    /// </remarks>
    public void VisitPropertyNode(PropertyNode node, in ReadOnlySpan<char> source)
    {
        // Handled within VisitClassNode
    }

    /// <summary>
    /// Visitor stub for a trait use declaration node.
    /// </summary>
    /// <param name="node">The <see cref="TraitUseNode"/> representing the trait use declaration.</param>
    /// <param name="source">The original source text.</param>
    /// <remarks>
    /// Trait member composition is handled entirely within <see cref="VisitClassNode"/> (Pass 0).
    /// </remarks>
    public void VisitTraitUseNode(TraitUseNode node, in ReadOnlySpan<char> source)
    {
        // Handled within VisitClassNode
    }

    /// <summary>
    /// Visitor stub for a class constant declaration node.
    /// </summary>
    /// <param name="node">The <see cref="ConstantNode"/> representing the constant declaration.</param>
    /// <param name="source">The original source text.</param>
    /// <remarks>
    /// Constant field definition and value assignment are handled entirely within
    /// <see cref="VisitClassNode"/> (Pass 1).
    /// </remarks>
    public void VisitConstantNode(ConstantNode node, in ReadOnlySpan<char> source)
    {
        // Handled within VisitClassNode
    }

    /// <summary>
    /// Resolves a <see cref="QualifiedNameNode"/> to its fully-qualified PHP name, applying
    /// namespace qualification and use-import aliasing as appropriate.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> to resolve; expected to be a <see cref="QualifiedNameNode"/>.</param>
    /// <param name="source">The original source text, used to extract identifier text values.</param>
    /// <returns>
    /// The fully-qualified name as a backslash-delimited string, or an empty string if
    /// <paramref name="node"/> is not a <see cref="QualifiedNameNode"/>.
    /// </returns>
    /// <remarks>
    /// Resolution priority: fully-qualified names (leading <c>\</c>) are returned as-is;
    /// <c>self</c> is returned unqualified; single-part names are checked against
    /// <c>_useImports</c> before falling back to namespace-prefixing; multi-part names are
    /// always prefixed with the current namespace if one is active.
    /// </remarks>
    private string ResolveFQN(SyntaxNode node, in ReadOnlySpan<char> source)
    {
        if (node is QualifiedNameNode qname)
        {
            var parts = new List<string>();
            foreach (var p in qname.Parts)
                parts.Add(p.TextValue(in source));

            string name = string.Join("\\", parts);
            if (qname.IsFullyQualified) return name;

            // Special handling for "self" - don't qualify it with namespace
            if (parts.Count == 1 && parts[0].Equals("self", StringComparison.OrdinalIgnoreCase))
            {
                return parts[0];
            }

            // Check use imports first
            if (parts.Count == 1 && _useImports.TryGetValue(parts[0], out var imported))
            {
                return imported;
            }

            return string.IsNullOrEmpty(_currentNamespace) ? name : _currentNamespace + "\\" + name;
        }
        return "";
    }
}