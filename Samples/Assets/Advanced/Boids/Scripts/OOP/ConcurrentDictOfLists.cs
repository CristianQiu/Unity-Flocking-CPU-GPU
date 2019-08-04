using System.Collections.Concurrent;
using System.Collections.Generic;

namespace BoidsOOP
{
    public class ConcurrentDictOfLists<T> where T : IHasheable
    {
        #region Private Attributes

        private const int DefaultCapacity = 4096;

        private ConcurrentDictionary<int, List<T>> dictLists = null;
        private ListPool<T> listPool = null;

        #endregion

        #region Properties

        public ConcurrentDictionary<int, List<T>> DictLists { get { return dictLists; } }

        #endregion

        #region Initialization

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="concurrencyLevel"></param>
        /// <returns></returns>
        public ConcurrentDictOfLists(int concurrencyLevel) : this(DefaultCapacity, concurrencyLevel)
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initialDictCapacity"></param>
        /// <param name="concurrencyLevel"></param>
        public ConcurrentDictOfLists(int initialDictCapacity, int concurrencyLevel)
        {
            dictLists = new ConcurrentDictionary<int, List<T>>(concurrencyLevel, initialDictCapacity);
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
            lock(listPool)
            {
                listPool.Push(l);
            }
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
                lock(listPool)
                {
                    list = listPool.Pop();
                }

                bool added = dictLists.TryAdd(hash, list);

                if (!added)
                {
                    lock(listPool)
                    {
                        listPool.Push(list);
                    }

                    return;
                }
            }

            lock(list)
            {
                list.Add(item);
            }
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
}