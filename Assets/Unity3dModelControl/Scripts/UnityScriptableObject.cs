using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityScriptableObject : ScriptableObject
{
    [SerializeField] private UnityEngine.Object[] objects;

    // Since it is not so good to search linearly every time you find it, it is cached as a cache in the Dictionary and it comes from there if it is in the cache.
    private Dictionary<string, UnityEngine.Object> loadObjectCacheDic = new Dictionary<string, UnityEngine.Object>();

    /// <summary>
    /// <para>Acquire the Prefab of the corresponding Prefab name</para>
    /// </summary>
    public T Find<T>(string name) where T : UnityEngine.Object
    {
        if (loadObjectCacheDic.ContainsKey(name))
        {
            return loadObjectCacheDic[name] as T;
        }
        UnityEngine.Object obj = null;
        UnityEngine.Object[] objs = objects;
        for (int i = 0; i < objs.Length; ++i)
        {
            if (objects[i].name == name)
            {
                obj = objs[i];
                break;
            }
        }
        if (obj == null) Debug.LogError("Not found - " + name);
        loadObjectCacheDic.Add(name, obj);
        return obj as T;
    }

    public T[] GetObjects<T>() where T : UnityEngine.Object{
        return Array.ConvertAll(objects, obj => obj as T);
    }

    public void SetObjects(UnityEngine.Object[] objects)
    {
        this.objects = objects;
    }

    /// <summary>
    /// <para>Unleash the reference of Prefab you have as Cache</para>
    /// </summary>
    public void CacheClear()
    {
        loadObjectCacheDic.Clear();
    }
}
