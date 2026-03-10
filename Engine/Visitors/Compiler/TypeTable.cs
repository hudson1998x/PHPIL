using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.Visitors
{
    public static class TypeTable
    {
        public static readonly Dictionary<string, Type> _types = new();

        static TypeTable()
        {
            // register core types
            _types["void"]   = typeof(void);
            _types["bool"]   = typeof(bool);
            _types["int"]    = typeof(int);
            _types["float"]  = typeof(double);
            _types["object"] = typeof(object);
            _types["mixed"]  = typeof(object);
            _types["array"]  = typeof(Dictionary<string, object>);
            _types["string"] = typeof(string);
        }

        /// <summary>
        /// Returns the C# type for a PHPIL AnalysedType
        /// </summary>
        public static Type GetPrimitive(AnalysedType type)
        {
            return _types[type.ToString().ToLower()];
        }
        
        private static readonly HashSet<Type> _primitiveTypes = new()
        {
            typeof(int),
            typeof(bool),
            typeof(double),
            typeof(string)
        };

        /// <summary>
        /// Returns true if the type is a "primitive" (int, bool, double, string)
        /// </summary>
        public static bool IsPrimitive(Type type)
        {
            return _primitiveTypes.Contains(type);
        }

        /// <summary>
        /// Emits IL to cast a primitive from one AnalysedType to a C# type
        /// </summary>
        public static void CastPrimitive(ILGenerator il, AnalysedType from, Type to)
        {
            if (to == typeof(string))
            {
                // box value types and call ToString
                Type primitiveType = GetPrimitive(from);
                if (primitiveType.IsValueType)
                    il.Emit(OpCodes.Box, primitiveType);

                il.Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString", Type.EmptyTypes)!);
                return;
            }

            if (to == typeof(int))
            {
                if (from == AnalysedType.Int) return;
                if (from == AnalysedType.Float) il.Emit(OpCodes.Conv_I4);
                if (from == AnalysedType.Boolean) il.Emit(OpCodes.Conv_I4);
                return;
            }

            if (to == typeof(double))
            {
                if (from == AnalysedType.Int) il.Emit(OpCodes.Conv_R8);
                if (from == AnalysedType.Float) return;
                return;
            }

            if (to == typeof(bool))
            {
                if (from == AnalysedType.Int || from == AnalysedType.Boolean) return;
                if (from == AnalysedType.Float) il.Emit(OpCodes.Conv_I4);
                return;
            }

            throw new NotImplementedException($"Cannot cast {from} to {to.Name}");
        }
    }
}