using System.Collections.Generic;
using UnityEngine;

public class ListPool<T>
{
    #region Private Attributes

    // kind of overkill numbers here but I don't want to see it break when changing the number of boids
    private const int DefaultListQuantity = 16384;
    private const int DefaultListCapacity = 2048;

    private List<List<T>> lists = null;

    #endregion

    #region Initialization

    /// <summary>
    /// Default constructor
    /// </summary>
    /// <returns></returns>
    public ListPool() : this(DefaultListQuantity, DefaultListCapacity)
    {

    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="listQuantity"></param>
    /// <param name="listCapacity"></param>
    public ListPool(int listQuantity, int listCapacity)
    {
        Init(listQuantity, listCapacity);
    }

    /// <summary>
    /// Initialize the pool
    /// </summary>
    /// <param name="listQuantity"></param>
    /// <param name="listCapacity"></param>
    private void Init(int listQuantity, int listCapacity)
    {
        lists = new List<List<T>>(listQuantity);

        for (int i = 0; i < listQuantity; i++)
        {
            List<T> nl = new List<T>(listCapacity);
            lists.Add(nl);
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Get a list from the pool
    /// </summary>
    /// <returns></returns>
    public List<T> Pop()
    {
        int index = lists.Count - 1;

        if (index < 0)
        {
            Debug.Log("Not enough lists in the pool, you might want to increase its size");
            return null;
        }

        List<T> l = lists[index];
        lists.RemoveAt(index);

        return l;
    }

    /// <summary>
    /// Return a list to the pool
    /// </summary>
    /// <param name="l"></param>
    public void Push(List<T> l)
    {
        lists.Add(l);
    }

    #endregion
}