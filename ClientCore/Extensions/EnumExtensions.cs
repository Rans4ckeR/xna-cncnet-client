using System;

namespace ClientCore.Extensions;

public static class EnumExtensions
{
    public static T Next<T>(this T src)
        where T : Enum
    {
        T[] arr = GetValues(src);
        int nextIndex = Array.IndexOf(arr, src) + 1;
        return arr.Length == nextIndex ? arr[0] : arr[nextIndex];
    }

    public static T First<T>(this T src)
        where T : Enum
    {
        return GetValues(src)[0];
    }

    private static T[] GetValues<T>(T src)
        where T : Enum
    {
        return (T[])Enum.GetValues(src.GetType());
    }
}