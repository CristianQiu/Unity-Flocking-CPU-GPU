using System.Collections.Generic;

public interface IHasheable
{
    int Hash();
}

public class DictionaryOfLists<T> where T : IHasheable
{
    #region Private Attributes

    private const int DefaultCapacity = 4096;

    private Dictionary<int, List<T>> dictLists = null;
    private ListPool<T> listPool = null;

    #endregion

    #region Properties

    public Dictionary<int, List<T>> DictLists { get { return dictLists; } }

    #endregion

    #region Initialization

    /// <summary>
    /// Default constructor
    /// </summary>
    /// <returns></returns>
    public DictionaryOfLists() : this(DefaultCapacity)
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="initialDictCapacity"></param>
    public DictionaryOfLists(int initialDictCapacity)
    {
        dictLists = new Dictionary<int, List<T>>(initialDictCapacity);
        listPool = new ListPool<T>();
    }

    #endregion

    #region Methods

    /// <summary>
    /// Return a list to the pool
    /// </summary>
    /// <param name="l"></param>
    public void PushList(List<T> l)
    {
        listPool.Push(l);
    }

    /// <summary>
    /// Add an item
    /// </summary>
    /// <param name="item"></param>
    public void Add(T item)
    {
        int hash = item.Hash();
        bool alreadyIn = dictLists.TryGetValue(hash, out List<T> list);

        if (!alreadyIn)
        {
            list = listPool.Pop();
            dictLists.Add(hash, list);
        }

        list.Add(item);
    }

    /// <summary>
    /// Clear the dictionary
    /// </summary>
    public void Clear()
    {
        dictLists.Clear();
    }

    #endregion
}