using System;

namespace PHPIL.Engine.SyntaxTree.Structure.OOP
{
    [Flags]
    public enum PhpModifiers : byte
    {
        None = 0,
        Public = 1 << 0,
        Protected = 1 << 1,
        Private = 1 << 2,
        Static = 1 << 3,
        Final = 1 << 4,
        Abstract = 1 << 5,
        Readonly = 1 << 6
    }
}
