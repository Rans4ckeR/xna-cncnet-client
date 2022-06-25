using System;
using System.Collections;
using System.Collections.Generic;

namespace DTAClient.Online;

/// <summary>
/// A custom collection that aims to provide quick insertion, removal and lookup operations while
/// always keeping the list sorted by combining Dictionary and LinkedList.
/// </summary>
/// <typeparam name="T">Type.</typeparam>
public class SortedUserCollection<T> : IUserCollection<T>
{
    private readonly Dictionary<string, LinkedListNode<T>> dictionary;

    private readonly LinkedList<T> linkedList;

    private readonly Func<T, T, int> userComparer;

    public SortedUserCollection(Func<T, T, int> userComparer)
    {
        dictionary = new Dictionary<string, LinkedListNode<T>>();
        linkedList = new LinkedList<T>();
        this.userComparer = userComparer;
    }

    public int Count => dictionary.Count;

    bool ICollection<T>.IsReadOnly => throw new NotImplementedException();

    public void Add(string username, T item)
    {
        if (linkedList.Count == 0)
        {
            LinkedListNode<T> node = linkedList.AddFirst(item);
            dictionary.Add(username.ToLower(), node);
            return;
        }

        LinkedListNode<T> currentNode = linkedList.First;
        while (true)
        {
            if (userComparer(currentNode.Value, item) > 0)
            {
                LinkedListNode<T> node = linkedList.AddBefore(currentNode, item);
                dictionary.Add(username.ToLower(), node);
                break;
            }

            if (currentNode.Next == null)
            {
                LinkedListNode<T> node = linkedList.AddAfter(currentNode, item);
                dictionary.Add(username.ToLower(), node);
                break;
            }

            currentNode = currentNode.Next;
        }
    }

    void ICollection<T>.Add(T item)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        linkedList.Clear();
        dictionary.Clear();
    }

    bool ICollection<T>.Contains(T item)
    {
        throw new NotImplementedException();
    }

    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public void DoForAllUsers(Action<T> action)
    {
        LinkedListNode<T> current = linkedList.First;
        while (current != null)
        {
            action(current.Value);
            current = current.Next;
        }
    }

    public T Find(string username)
    {
        if (dictionary.TryGetValue(username.ToLower(), out LinkedListNode<T> node))
            return node.Value;

        return default;
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public LinkedListNode<T> GetFirst() => linkedList.First;

    public void Reinsert(string username)
    {
        T existing = Find(username.ToLower());
        if (existing == null)
            return;

        _ = Remove(username);
        Add(username, existing);
    }

    public bool Remove(string username)
    {
        if (dictionary.TryGetValue(username.ToLower(), out LinkedListNode<T> node))
        {
            linkedList.Remove(node);
            _ = dictionary.Remove(username.ToLower());
            return true;
        }

        return false;
    }

    bool ICollection<T>.Remove(T item)
    {
        throw new NotImplementedException();
    }
}