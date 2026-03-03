using System;

namespace PHPIL.Engine.Runtime.Types
{
	public class PhpValue : IEquatable<PhpValue>
	{
		#region Fields & Static Values

		public object? Value { get; }

		public PhpType Type => ReferenceEquals(this, Void) ? PhpType.Void : Value switch
		{
			null => PhpType.Null,
			int => PhpType.Int,
			double => PhpType.Double,
			bool => PhpType.Bool,
			string => PhpType.String,
			_ => PhpType.Object
		};

		public bool IsVoid => ReferenceEquals(this, Void);

		// FIX: Added explicit cast to allow null in the constructor
		public static readonly PhpValue Null = new PhpValue((object?)null);

		public static readonly PhpValue Void = new PhpValue(new object());

		#endregion

		#region Constructors

		// FIX: Ensure constructor explicitly accepts object?
		public PhpValue(object? value) => Value = value;
		public PhpValue(int value) => Value = value;
		public PhpValue(double value) => Value = value;
		public PhpValue(bool value) => Value = value;
		public PhpValue(string value) => Value = value;

		#endregion

		#region Conversions
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
			if (ReferenceEquals(a, Void) || ReferenceEquals(b, Void)) throw new InvalidOperationException("Cannot add void values.");
			if (a.Type == PhpType.String || b.Type == PhpType.String) return new PhpValue(a.ToStringValue() + b.ToStringValue());
			return new PhpValue(a.ToDouble() + b.ToDouble());
		}
		public static PhpValue operator -(PhpValue a, PhpValue b) => new PhpValue(a.ToDouble() - b.ToDouble());
		public static PhpValue operator *(PhpValue a, PhpValue b) => new PhpValue(a.ToDouble() * b.ToDouble());
		public static PhpValue operator /(PhpValue a, PhpValue b) => new PhpValue(a.ToDouble() / b.ToDouble());
		public static PhpValue operator %(PhpValue a, PhpValue b) => new PhpValue(a.ToInt() % b.ToInt());
		#endregion

		#region Comparison
		public static bool operator <(PhpValue a, PhpValue b) => a.ToDouble() < b.ToDouble();
		public static bool operator >(PhpValue a, PhpValue b) => a.ToDouble() > b.ToDouble();
		public static bool operator <=(PhpValue a, PhpValue b) => a.ToDouble() <= b.ToDouble();
		public static bool operator >=(PhpValue a, PhpValue b) => a.ToDouble() >= b.ToDouble();
		public static PhpValue Spaceship(PhpValue a, PhpValue b)
		{
			var result = a.ToDouble().CompareTo(b.ToDouble());
			return new PhpValue(result < 0 ? -1 : result > 0 ? 1 : 0);
		}
		#endregion

		#region Logical & Equality Methods
		public static PhpValue And(PhpValue a, PhpValue b) => new PhpValue(a.ToBool() && b.ToBool());
		public static PhpValue Or(PhpValue a, PhpValue b) => new PhpValue(a.ToBool() || b.ToBool());
		public static PhpValue LooseEquals(PhpValue a, PhpValue b) => new PhpValue(a == b);
		public static PhpValue LooseNotEquals(PhpValue a, PhpValue b) => new PhpValue(a != b);
		public static PhpValue StrictEquals(PhpValue a, PhpValue b) => new PhpValue(a.Type == b.Type && a == b);
		public static PhpValue StrictNotEquals(PhpValue a, PhpValue b) => new PhpValue(a.Type != b.Type || a != b);
		#endregion

		#region Bitwise
		public static PhpValue operator &(PhpValue a, PhpValue b) => new PhpValue(a.ToInt() & b.ToInt());
		public static PhpValue operator |(PhpValue a, PhpValue b) => new PhpValue(a.ToInt() | b.ToInt());
		public static PhpValue operator ^(PhpValue a, PhpValue b) => new PhpValue(a.ToInt() ^ b.ToInt());
		public static PhpValue ShiftLeft(PhpValue a, PhpValue b) => new PhpValue(a.ToInt() << b.ToInt());
		public static PhpValue ShiftRight(PhpValue a, PhpValue b) => new PhpValue(a.ToInt() >> b.ToInt());
		#endregion

		public static PhpValue Concat(PhpValue a, PhpValue b) => new PhpValue(a.ToStringValue() + b.ToStringValue());

		public override string ToString() => ReferenceEquals(this, Void) ? "void" : Value?.ToString() ?? "null";
	}

	public enum PhpType { Void, Null, Int, Double, Bool, String, Object }
}