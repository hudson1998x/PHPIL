namespace PHPIL.Engine.Runtime;

public static class PhpOperators
{
    public static object Multiply(object left, object right)
    {
        if (left is double ld || right is double rd)
            return Convert.ToDouble(left) * Convert.ToDouble(right);
        return Convert.ToInt32(left) * Convert.ToInt32(right);
    }

    public static object Add(object left, object right)
    {
        if (left is double || right is double)
            return Convert.ToDouble(left) + Convert.ToDouble(right);
        return Convert.ToInt32(left) + Convert.ToInt32(right);
    }

    public static object Subtract(object left, object right)
    {
        if (left is double || right is double)
            return Convert.ToDouble(left) - Convert.ToDouble(right);
        return Convert.ToInt32(left) - Convert.ToInt32(right);
    }

    public static object Divide(object left, object right)
    {
        return Convert.ToDouble(left) / Convert.ToDouble(right);
    }

    public static object Modulo(object left, object right)
    {
        return Convert.ToInt32(left) % Convert.ToInt32(right);
    }
}