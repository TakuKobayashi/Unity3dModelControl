using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityScriptableObject : ScriptableObject
{
    [SerializeField] private UnityEngine.Object[] objects;

    // Findする時に毎回、線形的に探すのはあまり良くないので、Dictionaryにキャッシュとしてためて、キャッシュにあればそこから取ってくる。(速度的にはDictionary > Listのため)
    private Dictionary<string, UnityEngine.Object> loadObjectCacheDic = new Dictionary<string, UnityEngine.Object>();

    /// <summary>
    /// <para>該当のPrefab名のPrefabを取得する</para>
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
    /// <para>Cacheとして持っているPrefabの参照を解き放つ</para>
    /// <para>※参照を解き放ったらGCが走ったときにメモリをクリアにしてくれるはず??</para>
    /// </summary>
    public void CacheClear()
    {
        loadObjectCacheDic.Clear();
    }
}
