using System;

namespace PHPIL.Engine.Runtime.Types
{
    /// <summary>
    /// Represents a PHP runtime value in the PHPIL engine.
    /// Encapsulates any PHP type (int, double, bool, string, object, null, or void)
    /// and provides conversions, arithmetic, comparison, logical, bitwise, and concatenation operations.
    /// </summary>
    public class PhpValue : IEquatable<PhpValue>
    {
        #region Fields & Static Values

        /// <summary>
        /// The underlying .NET object representing the value.
        /// May be <c>null</c> or any primitive/object type.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// The PHP type of this value.
        /// </summary>
        public PhpType Type => ReferenceEquals(this, Void) ? PhpType.Void : Value switch
        {
            null => PhpType.Null,
            int => PhpType.Int,
            double => PhpType.Double,
            bool => PhpType.Bool,
            string => PhpType.String,
            _ => PhpType.Object
        };

        /// <summary>
        /// Returns true if this instance represents <see cref="Void"/>.
        /// </summary>
        public bool IsVoid => ReferenceEquals(this, Void);

        /// <summary>
        /// Represents the PHP <c>null</c> value.
        /// </summary>
        public static readonly PhpValue Null = new PhpValue(null);

        /// <summary>
        /// Represents a PHP <c>void</c> value.
        /// </summary>
        public static readonly PhpValue Void = new PhpValue(new object());

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="PhpValue"/> wrapping any object.
        /// </summary>
        public PhpValue(object? value) => Value = value;

        /// <summary>Creates a PhpValue from an int.</summary>
        public PhpValue(int value) => Value = value;

        /// <summary>Creates a PhpValue from a double.</summary>
        public PhpValue(double value) => Value = value;

        /// <summary>Creates a PhpValue from a bool.</summary>
        public PhpValue(bool value) => Value = value;

        /// <summary>Creates a PhpValue from a string.</summary>
        public PhpValue(string value) => Value = value;

        #endregion

        #region Conversions

        /// <summary>Converts the PhpValue to an int according to PHP type rules.</summary>
        /// <exception cref="InvalidOperationException">Thrown if the value is void.</exception>
        /// <exception cref="InvalidCastException">Thrown if conversion is not possible.</exception>
        public int ToInt()
        {
            if (ReferenceEquals(this, Void)) throw new InvalidOperationException("Cannot convert void to int.");
            if (Value == null) return 0;
            return Value switch
            {
                int i => i,
                double d => (int)d,
                bool b => b ? 1 : 0,
                string s when int.TryParse(s, out int res) => res,
                _ => throw new InvalidCastException($"Cannot convert {Type} to int.")
            };
        }

        /// <summary>Converts the PhpValue to a double according to PHP type rules.</summary>
        /// <exception cref="InvalidOperationException">Thrown if the value is void.</exception>
        /// <exception cref="InvalidCastException">Thrown if conversion is not possible.</exception>
        public double ToDouble()
        {
            if (ReferenceEquals(this, Void)) throw new InvalidOperationException("Cannot convert void to double.");
            if (Value == null) return 0;
            return Value switch
            {
                double d => d,
                int i => i,
                bool b => b ? 1 : 0,
                string s when double.TryParse(s, out double res) => res,
                _ => throw new InvalidCastException($"Cannot convert {Type} to double.")
            };
        }

        /// <summary>Converts the PhpValue to a bool according to PHP type rules.</summary>
        /// <exception cref="InvalidOperationException">Thrown if the value is void.</exception>
        public bool ToBool()
        {
            if (ReferenceEquals(this, Void)) throw new InvalidOperationException("Cannot convert void to bool.");
            if (Value == null) return false;
            return Value switch
            {
                bool b => b,
                int i => i != 0,
                double d => Math.Abs(d) > double.Epsilon,
                string s => !string.IsNullOrEmpty(s),
                _ => true
            };
        }

        /// <summary>Converts the PhpValue to a string according to PHP type rules.</summary>
        /// <exception cref="InvalidOperationException">Thrown if the value is void.</exception>
        public string ToStringValue()
        {
            if (ReferenceEquals(this, Void)) throw new InvalidOperationException("Cannot convert void to string.");
            return Value?.ToString() ?? string.Empty;
        }

        #endregion

        #region Equality

        public override bool Equals(object? obj) => Equals(obj as PhpValue);

        public bool Equals(PhpValue? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, Void) || ReferenceEquals(other, Void))
                return ReferenceEquals(this, Void) && ReferenceEquals(other, Void);
            if (Value == null && other.Value == null) return true;
            return Value?.Equals(other.Value) ?? false;
        }

        public override int GetHashCode() => ReferenceEquals(this, Void) ? int.MinValue : Value?.GetHashCode() ?? 0;

        public static bool operator ==(PhpValue? a, PhpValue? b) => a?.Equals(b) ?? b is null;
        public static bool operator !=(PhpValue? a, PhpValue? b) => !(a == b);

        #endregion

        #region Arithmetic Operators

        public static PhpValue operator +(PhpValue a, PhpValue b)
        {
            if (ReferenceEquals(a, Void) || ReferenceEquals(b, Void))
                throw new InvalidOperationException("Cannot add void values.");
            if (a.Type == PhpType.String || b.Type == PhpType.String)
                return new PhpValue(a.ToStringValue() + b.ToStringValue());
            return new PhpValue(a.ToDouble() + b.ToDouble());
        }

        public static PhpValue operator -(PhpValue a, PhpValue b)
        {
            if (ReferenceEquals(a, Void) || ReferenceEquals(b, Void))
                throw new InvalidOperationException("Cannot subtract void values.");
            return new PhpValue(a.ToDouble() - b.ToDouble());
        }

        public static PhpValue operator *(PhpValue a, PhpValue b)
        {
            if (ReferenceEquals(a, Void) || ReferenceEquals(b, Void))
                throw new InvalidOperationException("Cannot multiply void values.");
            return new PhpValue(a.ToDouble() * b.ToDouble());
        }

        public static PhpValue operator /(PhpValue a, PhpValue b)
        {
            if (ReferenceEquals(a, Void) || ReferenceEquals(b, Void))
                throw new InvalidOperationException("Cannot divide void values.");
            return new PhpValue(a.ToDouble() / b.ToDouble());
        }

        public static PhpValue operator %(PhpValue a, PhpValue b)
        {
            if (ReferenceEquals(a, Void) || ReferenceEquals(b, Void))
                throw new InvalidOperationException("Cannot modulo void values.");
            return new PhpValue(a.ToInt() % b.ToInt());
        }

        #endregion

        #region Comparison Operators

        public static PhpValue operator <(PhpValue a, PhpValue b)
        {
            if (ReferenceEquals(a, Void) || ReferenceEquals(b, Void))
                throw new InvalidOperationException("Cannot compare void values.");
            return new PhpValue(a.ToDouble() < b.ToDouble());
        }

        public static PhpValue operator >(PhpValue a, PhpValue b)
        {
            if (ReferenceEquals(a, Void) || ReferenceEquals(b, Void))
                throw new InvalidOperationException("Cannot compare void values.");
            return new PhpValue(a.ToDouble() > b.ToDouble());
        }

        public static PhpValue operator <=(PhpValue a, PhpValue b)
        {
            if (ReferenceEquals(a, Void) || ReferenceEquals(b, Void))
                throw new InvalidOperationException("Cannot compare void values.");
            return new PhpValue(a.ToDouble() <= b.ToDouble());
        }

        public static PhpValue operator >=(PhpValue a, PhpValue b)
        {
            if (ReferenceEquals(a, Void) || ReferenceEquals(b, Void))
                throw new InvalidOperationException("Cannot compare void values.");
            return new PhpValue(a.ToDouble() >= b.ToDouble());
        }

        /// <summary>Implements the PHP spaceship operator (&lt;=&gt;).</summary>
        public static PhpValue Spaceship(PhpValue a, PhpValue b)
        {
            if (ReferenceEquals(a, Void) || ReferenceEquals(b, Void))
                throw new InvalidOperationException("Cannot compare void values.");
            var result = a.ToDouble().CompareTo(b.ToDouble());
            return new PhpValue(result < 0 ? -1 : result > 0 ? 1 : 0);
        }

        #endregion

        #region Logical Operators

        public static PhpValue And(PhpValue a, PhpValue b) => new PhpValue(a.ToBool() && b.ToBool());
        public static PhpValue Or(PhpValue a, PhpValue b) => new PhpValue(a.ToBool() || b.ToBool());
        public static PhpValue Not(PhpValue a) => new PhpValue(!a.ToBool());

        #endregion

        #region Equality Methods

        public static PhpValue LooseEquals(PhpValue a, PhpValue b) => new PhpValue(a == b);
        public static PhpValue LooseNotEquals(PhpValue a, PhpValue b) => new PhpValue(a != b);
        public static PhpValue StrictEquals(PhpValue a, PhpValue b) => new PhpValue(a.Type == b.Type && a == b);
        public static PhpValue StrictNotEquals(PhpValue a, PhpValue b) => new PhpValue(a.Type != b.Type || a != b);

        #endregion

        #region Bitwise Operators

        public static PhpValue operator &(PhpValue a, PhpValue b)
        {
            if (ReferenceEquals(a, Void) || ReferenceEquals(b, Void))
                throw new InvalidOperationException("Cannot bitwise AND void values.");
            return new PhpValue(a.ToInt() & b.ToInt());
        }

        public static PhpValue operator |(PhpValue a, PhpValue b)
        {
            if (ReferenceEquals(a, Void) || ReferenceEquals(b, Void))
                throw new InvalidOperationException("Cannot bitwise OR void values.");
            return new PhpValue(a.ToInt() | b.ToInt());
        }

        public static PhpValue operator ^(PhpValue a, PhpValue b)
        {
            if (ReferenceEquals(a, Void) || ReferenceEquals(b, Void))
                throw new InvalidOperationException("Cannot bitwise XOR void values.");
            return new PhpValue(a.ToInt() ^ b.ToInt());
        }

        public static PhpValue operator ~(PhpValue a)
        {
            if (ReferenceEquals(a, Void))
                throw new InvalidOperationException("Cannot bitwise NOT void value.");
            return new PhpValue(~a.ToInt());
        }

        public static PhpValue ShiftLeft(PhpValue a, PhpValue b) => new PhpValue(a.ToInt() << b.ToInt());
        public static PhpValue ShiftRight(PhpValue a, PhpValue b) => new PhpValue(a.ToInt() >> b.ToInt());
        public static PhpValue ShiftRightUnsigned(PhpValue a, PhpValue b) => new PhpValue((int)((uint)a.ToInt() >> b.ToInt()));

        #endregion

        #region String Concatenation

        /// <summary>
        /// Concatenates two <see cref="PhpValue"/> instances as strings, similar to PHP's <c>.</c> operator.
        /// </summary>
        /// <param name="a">Left-hand operand.</param>
        /// <param name="b">Right-hand operand.</param>
        /// <returns>A new <see cref="PhpValue"/> containing the string result of <c>a</c> + <c>b</c>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if either operand is <see cref="Void"/>.</exception>
        public static PhpValue Concat(PhpValue a, PhpValue b)
        {
            if (ReferenceEquals(a, Void) || ReferenceEquals(b, Void))
                throw new InvalidOperationException("Cannot concatenate void values.");

            return new PhpValue(a.ToStringValue() + b.ToStringValue());
        }

        #endregion

        #region Debug

        public override string ToString() => ReferenceEquals(this, Void) ? "void" : Value?.ToString() ?? "null";
        public string DebugInfo() => ReferenceEquals(this, Void) ? "Void" : $"{Type}({Value ?? "null"})";

        #endregion
    }

    /// <summary>
    /// Enumeration of PHP value types.
    /// </summary>
    public enum PhpType
    {
        Void,
        Null,
        Int,
        Double,
        Bool,
        String,
        Object
    }
}