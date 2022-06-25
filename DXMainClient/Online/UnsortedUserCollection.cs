using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DTAClient.Online;

/// <summary>
/// A custom collection that aims to provide quick insertion,
/// removal and lookup operations by using a dictionary. Does not
/// keep the list sorted.
/// </summary>
/// <typeparam name="T">Type.</typeparam>
public class UnsortedUserCollection<T> : IUserCollection<T>
{
    private readonly Dictionary<string, T> dictionary = new();

    public int Count => dictionary.Count;

    bool ICollection<T>.IsReadOnly => throw new NotImplementedException();

    public void Add(string username, T item)
    {
        dictionary.Add(username.ToLower(), item);
    }

    public void Clear()
    {
        dictionary.Clear();
    }

    public void DoForAllUsers(Action<T> action)
    {
        Dictionary<string, T>.ValueCollection values = dictionary.Values;

        foreach (T value in values)
        {
            action(value);
        }
    }

    public T Find(string username)
    {
        if (dictionary.TryGetValue(username.ToLower(), out T value))
            return value;

        return default;
    }

    public LinkedListNode<T> GetFirst()
    {
        throw new NotImplementedException();
    }

    public void Reinsert(string username)
    {
        throw new NotImplementedException();
    }

    public bool Remove(string username)
    {
        return dictionary.Remove(username.ToLower());
    }

    void ICollection<T>.Add(T item)
    {
        throw new NotImplementedException();
    }

    bool ICollection<T>.Contains(T item)
    {
        throw new NotImplementedException();
    }

    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    bool ICollection<T>.Remove(T item)
    {
        throw new NotImplementedException();
    }
}