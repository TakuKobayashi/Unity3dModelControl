using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScriptableGameObject : ScriptableObject
{
    public GameObject[] gameObjects;

    // Since it is not so good to search linearly every time you find it, it is cached as a cache in the Dictionary and it comes from there if it is in the cache.
    private Dictionary<string, GameObject> loadObjectCacheDic = new Dictionary<string, GameObject>();

    /// <summary>
    /// <para>Acquire the Prefab of the corresponding Prefab name</para>
    /// </summary>
    public GameObject Find(string name)
    {
        if (loadObjectCacheDic.ContainsKey(name))
        {
            return loadObjectCacheDic[name];
        }
        GameObject go = null;
        for (int i = 0; i < gameObjects.Length; ++i)
        {
            if (gameObjects[i].name == name)
            {
                go = gameObjects[i];
                break;
            }
        }
        if (go == null) Debug.LogError("Not found Prefab - " + name);
        loadObjectCacheDic.Add(name, go);
        return go;
    }

    /// <summary>
    /// <para>Unleash the reference of Prefab you have as Cache</para>
    /// </summary>
    public void CacheClear()
    {
        loadObjectCacheDic.Clear();
    }
}
