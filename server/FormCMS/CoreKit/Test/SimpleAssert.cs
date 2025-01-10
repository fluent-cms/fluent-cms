namespace FormCMS.CoreKit.Test;

// a lightweight assert Tool to avoid depending on a particular Test Framework,
public static class SimpleAssert
{
    public static void IsTrue(bool condition, string message = "Assertion failed: Condition is not true.")
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void IsFalse(bool condition, string message = "Assertion failed: Condition is not false.")
    {
        if (condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void AreEqual(object expected, object actual, string? message = null)
    {
        if (!Equals(expected, actual))
        {
            throw new InvalidOperationException(message ?? $"Assertion failed: Expected {expected}, but got {actual}.");
        }
    }

    public static void AreNotEqual(object notExpected, object actual, string? message = null)
    {
        if (Equals(notExpected, actual))
        {
            throw new InvalidOperationException(message ?? $"Assertion failed: Did not expect {notExpected}, but got {actual}.");
        }
    }

    public static void IsNull(object obj, string message = "Assertion failed: Object is not null.")
    {
        if (obj != null)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void IsNotNull(object obj, string message = "Assertion failed: Object is null.")
    {
        if (obj == null)
        {
            throw new InvalidOperationException(message);
        }
    }
}
