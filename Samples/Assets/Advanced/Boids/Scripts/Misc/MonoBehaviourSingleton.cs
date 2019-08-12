using System.Threading;
using UnityEngine;

/// <summary>
/// Class to inherit from in order to add singleton functionality. Note that it is only designed to be used at runtime and it is not thread safe.
/// Because of that it is recommended to not use this class to implement scripts that must interact with the editor.
/// </summary>
/// <typeparam name="T"></typeparam>
[DisallowMultipleComponent]
public abstract class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviourSingleton<T>
{
    #region Private Attributes

    private static T instance = null;
    private bool initialized = false;

    private static Thread mainThread = null;

    #endregion

    #region Properties

    public static T Instance
    {
        get
        {
#if UNITY_EDITOR
            // show a warning if called from the editor and not playing
            if (mainThread == Thread.CurrentThread && !Application.isPlaying)
            {
                Debug.LogWarningFormat("Trying to access a MonoBehaviour Singleton {0} in the editor while not playing. This is not allowed.", typeof(T).Name);
                return null;
            }
#endif
            // if not yet created try to find one
            if (instance == null)
            {
                string name = typeof(T).Name;
                T[] instances = FindObjectsOfType<T>();
                int length = instances.Length;

                if (instances != null && length > 0)
                {
                    instance = instances[0];

                    // deleted script duplicates will be restored once the game stops, let's show a warning before
                    if (length > 1)
                        Debug.LogWarningFormat("The MonoBehaviour Singleton {0} has several instanced scripts across the scene and some of them have been deleted. It is HIGHLY recommended that you only leave one instance of the script you want to use.", name);

                    // now delete any duplicate
                    for (int i = 1; i < length; i++)
                    {
                        T inst = instances[i];
                        Debug.LogWarningFormat("Destroying a MonoBehaviour Singleton {0} duplicate...", inst.name);
                        Destroy(inst);
                    }
                }

                // none found, create a new one
                if (instance == null)
                {
                    GameObject go = new GameObject(name);
                    instance = go.AddComponent<T>();
                }

                // because we are initializing on Awake(), we dont really need to force it again if already initialized
                instance.Init(false);
            }

            return instance;
        }
    }

    public bool Initialized { get { return initialized; } }

    protected abstract bool DestroyOnLoad { get; }

    #endregion

    #region MonoBehaviour Methods

    protected virtual void Awake()
    {
        mainThread = Thread.CurrentThread;

        // just a "warm up" so that we make sure that the singleton already exists when the game starts
        Instance.Init(false);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Called the very first time the singleton instance is accessed, and thus, lazily instanced.
    /// This is automatically called in Awake() to prepare the singleton instance.
    /// </summary>
    /// <param name="force"></param>
    /// <returns></returns>
    protected virtual bool Init(bool force)
    {
        if (initialized && !force)
            return false;

        if (!instance.DestroyOnLoad)
            DontDestroyOnLoad(instance);

        initialized = true;

        return true;
    }

    #endregion
}