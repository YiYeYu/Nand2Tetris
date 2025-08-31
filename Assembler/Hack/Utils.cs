namespace Hack;

public sealed class Utils
{
    public static char? TryGetStringCharAt(string str, int index)
    {
        if (index >= str.Length)
        {
            return null;
        }

        return str[index];
    }
}