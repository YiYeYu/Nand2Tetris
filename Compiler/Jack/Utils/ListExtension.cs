using System.Runtime.Intrinsics.X86;

namespace Jack;

public static class ListExtension
{
    public static void ForEach<T>(this List<T> list, Action<T> action)
    {
        foreach (var item in list)
        {
            action(item);
        }
    }

    public static void Push<T>(this List<T> list, T item) => list.Add(item);
    public static void Pop<T>(this List<T> list) => list.RemoveAt(list.Count - 1);
    public static T Peek<T>(this List<T> list, int index = 0) => list[list.Count - 1 - index];
}