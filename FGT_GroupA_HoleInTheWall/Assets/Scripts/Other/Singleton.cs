using System;
using UnityEngine;

public abstract class Singleton<T> where T : class, new()
{
    private static readonly object _lock = new object ();
    private static T? instance = null;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                lock (_lock)
                    {
                        if (instance == null)
                        {
                            instance = new T();
                        }
                    }
            }
            return instance;
        }
    }
}
