using System;
using System.Collections.Generic;

namespace DTAClient.Online;

public interface IUserCollection<T> : ICollection<T>
{
    void Add(string username, T item);

    void DoForAllUsers(Action<T> action);

    T Find(string username);

    LinkedListNode<T> GetFirst();

    void Reinsert(string username);

    bool Remove(string username);
}