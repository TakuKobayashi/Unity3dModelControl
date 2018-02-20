using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class ThreedObjectControlEditor
{

    public enum ExportImageFileExtention
    {
        png,
        jpg,
        exr
    }

    public enum SearchThreedObjectFileExtention
    {
        fbx,
        dae,
        dxf,
        obj,
        skp,
        mb,
        ma,
        blend,
        c4d,
        max,
        threeds,
        prefab
    }

    public enum ExportReferenceFileExtention
    {
        asset,
        csv,
        json
    }

    public enum FilterMeshRendererTypes
    {
        OnlySkinnedMeshRenderer,
        OnlyMeshRenderer,
        BothMeshRenderer
    }

    public enum AttachColliderTypes
    {
        BoxCollider,
        CapsuleCollider,
        MeshCollider,
        SphereCollider
    }

    public enum RegisterFileType
    {
        all,
        prefab,
        anim,
        sprite,
        audio
    }

    public static void ConvertToPrefab(string searchRootDirectory, string exportDirectoryPath, SearchThreedObjectFileExtention searchFileExtention = SearchThreedObjectFileExtention.fbx, bool isExportMaterialFiles = true, bool distoributeParentFlag = false, int hierarchyNumber = 1)
    {
        List<string> pathes = FindAllThreedSearchDirectory(searchRootDirectory, searchFileExtention);

        Dictionary<string, GameObject> pathToThreedObjects = new Dictionary<string, GameObject>();
        Dictionary<string, List<Renderer>> pathToRendererList = new Dictionary<string, List<Renderer>>();

        for (int i = 0; i < pathes.Count; ++i)
        {
            string path = pathes[i];
            GameObject threedObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (threedObject == null) continue;
            pathToThreedObjects.Add(path, threedObject);
            if (isExportMaterialFiles)
            {
                pathToRendererList.Add(path, FindAllCompomentInChildren<Renderer>(threedObject.transform));
            }
        }

        Dictionary<string, Material> generatedMaterialFiles = new Dictionary<string, Material>();
        Dictionary<Renderer, HashSet<Material>> attachDic = new Dictionary<Renderer, HashSet<Material>>();
        List<GameObject> generatedPrefabs = new List<GameObject>();

        foreach (KeyValuePair<string, GameObject> pathThreedObject in pathToThreedObjects)
        {
            string prefabFilePath = SetupAndGetPlaneFilePath(exportDirectoryPath, pathThreedObject.Key, distoributeParentFlag, hierarchyNumber) + ".prefab";
            if (File.Exists(prefabFilePath)) continue;
            GameObject generatedPrefab = PrefabUtility.CreatePrefab(prefabFilePath, pathThreedObject.Value);
            generatedPrefabs.Add(generatedPrefab);

            if (isExportMaterialFiles)
            {
                //Prefabとして作成したものにあるRendererのListを取得する
                List<Renderer> newRenderers = FindAllCompomentInChildren<Renderer>(generatedPrefab.transform);
                if(generatedPrefab.GetComponent<Renderer>() != null){
                    newRenderers.Add(generatedPrefab.GetComponent<Renderer>());
                }
                List<string> splitPathes = new List<string>(prefabFilePath.Split("/".ToCharArray()));
                int joinArrayCount = Mathf.Max(splitPathes.Count - 1, 0);
                if (distoributeParentFlag)
                {
                    joinArrayCount = Mathf.Max(splitPathes.Count - 2, 0);
                }
                string rootMaterialDirectoryPath = string.Join("/", splitPathes.GetRange(0, joinArrayCount).ToArray()) + "/Materials/";
                List<Renderer> originRenderers = pathToRendererList[pathThreedObject.Key];
                for (int i = 0; i < originRenderers.Count; ++i)
                {
                    Material[] mats = originRenderers[i].sharedMaterials;
                    if (mats != null)
                    {
                        HashSet<Material> copyMaterials = new HashSet<Material>();
                        for (int j = 0; j < mats.Length; ++j)
                        {
                            string materialFilePath = SetupAndGetPlaneFilePath(rootMaterialDirectoryPath, mats[j].name) + ".mat";
                            if (File.Exists(materialFilePath))
                            {
                                copyMaterials.Add(generatedMaterialFiles[materialFilePath]);
                            }
                            else
                            {
                                Material copyMaterial = CopyAssetFile(materialFilePath, mats[j]);
                                copyMaterials.Add(copyMaterial);
                                generatedMaterialFiles.Add(materialFilePath, copyMaterial);
                            }
                        }
                        attachDic.Add(newRenderers[i], copyMaterials);
                    }
                }
            }
        }
        AssetDatabase.StartAssetEditing();
        foreach (KeyValuePair<Renderer, HashSet<Material>> newRendererMaterials in attachDic)
        {
            newRendererMaterials.Key.materials = newRendererMaterials.Value.ToArray();
        }
        AssetDatabase.StopAssetEditing();
        for (int i = 0; i < generatedPrefabs.Count; ++i)
        {
            EditorUtility.SetDirty(generatedPrefabs[i]);
        }
        foreach (KeyValuePair<Renderer, HashSet<Material>> newRendererMaterials in attachDic)
        {
            foreach (Material mat in newRendererMaterials.Value)
            {
                EditorUtility.SetDirty(mat);
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Convert ThreedObject To Prefab Count:" + generatedPrefabs.Count);
    }

    public static void DissociateAnimationClip(string searchRootDirectory, string exportDirectoryPath, bool distoributeParentFlag = false, SearchThreedObjectFileExtention searchFileExtention = SearchThreedObjectFileExtention.fbx, int hierarchyNumber = 1)
    {
        List<string> pathes = FindAllThreedSearchDirectory(searchRootDirectory, searchFileExtention);
        Dictionary<string, List<AnimationClip>> pathToAnimationClips = new Dictionary<string, List<AnimationClip>>();

        for (int i = 0; i < pathes.Count; ++i)
        {
            string path = pathes[i];
            List<AnimationClip> animationClips = new List<AnimationClip>();
            object[] threedObjects = AssetDatabase.LoadAllAssetsAtPath(path);
            if (threedObjects == null) continue;
            for (int j = 0; j < threedObjects.Length; ++j)
            {
                if (threedObjects[j] is AnimationClip)
                {
                    AnimationClip clip = threedObjects[j] as AnimationClip;
                    if (clip.name.StartsWith("__preview__"))
                    {
                        continue;
                    }
                    animationClips.Add(clip);
                }
            }
            if (animationClips.Count <= 0) continue;
            pathToAnimationClips.Add(path, animationClips);
        }

        List<AnimationClip> generatedClips = new List<AnimationClip>();
        foreach (KeyValuePair<string, List<AnimationClip>> pathClips in pathToAnimationClips)
        {
            string animRootFileNamePath = SetupAndGetPlaneFilePath(exportDirectoryPath, pathClips.Key, distoributeParentFlag, hierarchyNumber);
            for (int i = 0; i < pathClips.Value.Count; ++i)
            {
                string animFileName = animRootFileNamePath;
                if (i > 0)
                {
                    animFileName += i.ToString();
                }
                animFileName += ".anim";
                if (File.Exists(animFileName)) continue;
                generatedClips.Add(CopyAssetFile(animFileName, pathClips.Value[i]));
            }
        }
        for (int i = 0; i < generatedClips.Count; ++i)
        {
            EditorUtility.SetDirty(generatedClips[i]);
        }
        AssetDatabase.Refresh();
        Debug.Log("DissociateAnimationClip Count:" + generatedClips.Count);
    }

    // Generate 3D Objects Thumbnail
    public static void CaptureImage(string searchRootDirectory,
                                    string exportDirectoryPath,
                                    Camera mainCamera,
                                    int captureImageWidth,
                                    int captureImageHeight,
                                    bool distoributeParentFlag = true,
                                    int hierarchyNumber = 1,
                                    ExportImageFileExtention exportFileExtention = ExportImageFileExtention.png)
    {
        if (mainCamera == null)
        {
            Debug.LogError("MainCamera is None!!");
            return;
        }

        Dictionary<string, GameObject> pathToObjects = new Dictionary<string, GameObject>();

        List<string> pathes = FindAllThreedSearchDirectory(searchRootDirectory, SearchThreedObjectFileExtention.prefab);
        for (int i = 0; i < pathes.Count; ++i)
        {
            string path = pathes[i];
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;
            pathToObjects.Add(path, go);
        }

        foreach (KeyValuePair<string, GameObject> pathToObj in pathToObjects)
        {
            // Instantiate and adjust the orientation to a position that is easy to take
            GameObject unit = GameObject.Instantiate(pathToObj.Value, Vector3.zero, Quaternion.identity) as GameObject;
            unit.transform.eulerAngles = new Vector3(270.0f, 0.0f, 0.0f);

            Vector3 nowPos = mainCamera.transform.position;
            float nowSize = mainCamera.orthographicSize;

            // adjustment camera
            mainCamera.transform.position = new Vector3(nowPos.x, nowPos.y, nowPos.z);
            mainCamera.orthographicSize = captureImageWidth;

            // Generate RenderTexture and write what is shown in the current Scene to this
            RenderTexture renderTexture = new RenderTexture(captureImageWidth, captureImageHeight, 24);
            mainCamera.targetTexture = renderTexture;
            mainCamera.Render();
            RenderTexture.active = renderTexture;
            Texture2D texture2D = new Texture2D(captureImageWidth, captureImageHeight, TextureFormat.ARGB32, false);
            texture2D.ReadPixels(new Rect(0, 0, captureImageWidth, captureImageHeight), 0, 0);
            mainCamera.targetTexture = null;

            for (int y = 0; y < captureImageHeight; y++)
            {
                for (int x = 0; x < captureImageWidth; x++)
                {
                    Color c = texture2D.GetPixel(x, y);
                    c = new Color(c.r, c.g, c.b, c.a);
                    texture2D.SetPixel(x, y, c);
                }
            }

            // Output texture byte to file
            string saveFilePath = SetupAndGetPlaneFilePath(exportDirectoryPath, pathToObj.Key, distoributeParentFlag, hierarchyNumber) + "." + exportFileExtention.ToString();
            if (exportFileExtention == ExportImageFileExtention.jpg)
            {
                File.WriteAllBytes(saveFilePath, texture2D.EncodeToJPG());
            }
            else if (exportFileExtention == ExportImageFileExtention.png)
            {
                File.WriteAllBytes(saveFilePath, texture2D.EncodeToPNG());
            }
            else if (exportFileExtention == ExportImageFileExtention.exr)
            {
                File.WriteAllBytes(saveFilePath, texture2D.EncodeToEXR());
            }

            mainCamera.targetTexture = null;
            RenderTexture.active = null;
            renderTexture.Release();

            mainCamera.transform.position = nowPos;
            mainCamera.orthographicSize = nowSize;
            Resources.UnloadUnusedAssets();
            System.GC.Collect();

            GameObject.DestroyImmediate(unit);
        }

        Debug.Log("Captured:" + pathToObjects.Count);
        System.GC.Collect();
    }

    public static void RegisterAssetsReference(string searchRootDirectory,
                                               string exportDirectoryPath,
                                               string exportFilePrefix = "export",
                                               RegisterFileType registerFileType = RegisterFileType.all,
                                               bool distoributeParentFlag = false,
                                               int hierarchyNumber = 1,
                                               ThreedObjectControlEditor.ExportReferenceFileExtention exportFileExtention = ThreedObjectControlEditor.ExportReferenceFileExtention.asset)
    {
        Type filterClassType = typeof(UnityEngine.Object);
        string filterWord = "*";
        if (registerFileType == RegisterFileType.prefab)
        {
            filterWord = "prefab";
            filterClassType = typeof(GameObject);
        }
        else if (registerFileType == RegisterFileType.anim)
        {
            filterWord = "anim";
            filterClassType = typeof(AnimationClip);
        }
        else if (registerFileType == RegisterFileType.audio)
        {
            filterClassType = typeof(AudioClip);
        }
        else if (registerFileType == RegisterFileType.sprite)
        {
            filterClassType = typeof(Sprite);
        }
        List<string> pathes = FindAllThreedSearchDirectory(searchRootDirectory, filterWord);
        Dictionary<UnityEngine.Object, string> objectToPathes = new Dictionary<UnityEngine.Object, string>();
        for (int i = 0; i < pathes.Count; ++i)
        {
            string path = pathes[i];
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(path, filterClassType);
            if (obj == null) continue;
            objectToPathes.Add(obj, path);
        }
        CheckAndCreateDirectory(exportDirectoryPath);

        Dictionary<string, List<UnityEngine.Object>> filePrefixObjectList = new Dictionary<string, List<UnityEngine.Object>>();

        foreach (KeyValuePair<UnityEngine.Object, string> objectToPath in objectToPathes)
        {
            string[] splitPath = objectToPath.Value.Split("/".ToCharArray());
            string prefixFileName = exportFilePrefix;
            if (distoributeParentFlag && splitPath.Length > hierarchyNumber)
            {
                prefixFileName = splitPath[splitPath.Length - (hierarchyNumber + 1)];
            }
            string filePrefix = exportDirectoryPath + prefixFileName;
            if (!filePrefixObjectList.ContainsKey(filePrefix))
            {
                filePrefixObjectList.Add(filePrefix, new List<UnityEngine.Object>());
            }
            filePrefixObjectList[filePrefix].Add(objectToPath.Key);
        }

        foreach (KeyValuePair<string, List<UnityEngine.Object>> filePrefixObject in filePrefixObjectList)
        {
            filePrefixObject.Value.Sort((a, b) => string.Compare(a.name, b.name));
        }

        if (exportFileExtention == ThreedObjectControlEditor.ExportReferenceFileExtention.asset)
        {
            List<UnityEngine.Object> dbList = new List<UnityEngine.Object>();
            AssetDatabase.StartAssetEditing();
            foreach (KeyValuePair<string, List<UnityEngine.Object>> filePrefixObject in filePrefixObjectList)
            {
                UnityScriptableObject db = LoadOrCreateDB(filePrefixObject.Key + ".asset", typeof(UnityScriptableObject)) as UnityScriptableObject;
                db.SetObjects(filePrefixObject.Value.ToArray());
                dbList.Add(db);
            }
            AssetDatabase.StopAssetEditing();
            for (int i = 0; i < dbList.Count; ++i)
            {
                EditorUtility.SetDirty(dbList[i]);
            }
        }
        else if (exportFileExtention == ThreedObjectControlEditor.ExportReferenceFileExtention.csv || exportFileExtention == ThreedObjectControlEditor.ExportReferenceFileExtention.json)
        {
            foreach (KeyValuePair<string, List<UnityEngine.Object>> filePrefixObject in filePrefixObjectList)
            {
                List<string> filePathes = new List<string>();
                List<UnityEngine.Object> gameObjectList = filePrefixObject.Value.ToList();
                for (int i = 0; i < gameObjectList.Count; ++i)
                {
                    filePathes.Add(objectToPathes[gameObjectList[i]]);
                }

                if (exportFileExtention == ThreedObjectControlEditor.ExportReferenceFileExtention.csv)
                {
                    File.WriteAllText(filePrefixObject.Key + ".csv", string.Join("\n", filePathes.ToArray()));
                }
                else
                {
                    if (filePathes.Count > 0)
                    {
                        File.WriteAllText(filePrefixObject.Key + ".json", "[\"" + string.Join("\",\"", filePathes.ToArray()) + "\"]");
                    }
                    else
                    {
                        File.WriteAllText(filePrefixObject.Key + ".json", "[]");
                    }
                }
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Register Object Count:" + objectToPathes.Count);
    }

    public static void AttachColliderMaxVolumn(string searchRootDirectory,
                                               ThreedObjectControlEditor.FilterMeshRendererTypes filterMeshRendererTypes = ThreedObjectControlEditor.FilterMeshRendererTypes.OnlySkinnedMeshRenderer,
                                               ThreedObjectControlEditor.AttachColliderTypes attachColliderTypes = ThreedObjectControlEditor.AttachColliderTypes.BoxCollider)
    {
        List<string> pathes = FindAllThreedSearchDirectory(searchRootDirectory, SearchThreedObjectFileExtention.prefab);
        Dictionary<GameObject, Renderer> objectRenderers = new Dictionary<GameObject, Renderer>();
        for (int i = 0; i < pathes.Count; ++i)
        {
            string path = pathes[i];
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;
            List<Renderer> renderers = LoadMeshRenderers(go, filterMeshRendererTypes);
            if (renderers == null || renderers.Count <= 0) continue;
            float maxVolumn = float.MinValue;
            int targetIndex = -1;
            for (int j = 0; j < renderers.Count; ++j)
            {
                Vector3 cubeSize = renderers[j].bounds.extents * 2;
                if (maxVolumn < (cubeSize.x * cubeSize.y * cubeSize.z))
                {
                    maxVolumn = (cubeSize.x * cubeSize.y * cubeSize.z);
                    targetIndex = j;
                }
            }
            if (targetIndex < 0) continue;
            objectRenderers.Add(go, renderers[targetIndex]);
        }
        AssetDatabase.StartAssetEditing();
        foreach (KeyValuePair<GameObject, Renderer> objectRenderer in objectRenderers)
        {
            GameObject go = objectRenderer.Key;
            Renderer meshRenderer = objectRenderer.Value;
            Bounds bounds = meshRenderer.bounds;
            if (attachColliderTypes == ThreedObjectControlEditor.AttachColliderTypes.BoxCollider)
            {
                BoxCollider boxCollider = go.GetComponent<BoxCollider>();
                if (boxCollider.Equals(null))
                {
                    boxCollider = go.AddComponent<BoxCollider>();
                }
                boxCollider.center = bounds.center;
                boxCollider.size = bounds.extents * 2;
            }
            else if (attachColliderTypes == ThreedObjectControlEditor.AttachColliderTypes.CapsuleCollider)
            {
                CapsuleCollider capsuleCollider = go.GetComponent<CapsuleCollider>();
                if (capsuleCollider.Equals(null))
                {
                    capsuleCollider = go.AddComponent<CapsuleCollider>();
                }
                capsuleCollider.center = bounds.center;
                capsuleCollider.height = Mathf.Abs(bounds.extents.y * 2);
                capsuleCollider.radius = Mathf.Max(Mathf.Abs(bounds.extents.x), Mathf.Abs(bounds.extents.z));
            }
            else if (attachColliderTypes == ThreedObjectControlEditor.AttachColliderTypes.SphereCollider)
            {
                SphereCollider sphereCollider = go.GetComponent<SphereCollider>();
                if (sphereCollider.Equals(null))
                {
                    sphereCollider = go.AddComponent<SphereCollider>();
                }
                sphereCollider.center = bounds.center;
                sphereCollider.radius = Mathf.Max(Mathf.Max(Mathf.Abs(bounds.extents.x), Mathf.Abs(bounds.extents.y)), Mathf.Abs(bounds.extents.z));
            }
            else if (attachColliderTypes == ThreedObjectControlEditor.AttachColliderTypes.MeshCollider)
            {
                MeshCollider meshCollider = go.GetComponent<MeshCollider>();
                if (meshCollider.Equals(null))
                {
                    meshCollider = go.AddComponent<MeshCollider>();
                }
            }
        }

        AssetDatabase.StopAssetEditing();
        foreach (KeyValuePair<GameObject, Renderer> objectMeshRenderer in objectRenderers)
        {
            EditorUtility.SetDirty(objectMeshRenderer.Key);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Attach and edit the max volumn colliders:" + objectRenderers.Count);
    }

    public static void AttachColliderWiddestSize(string searchRootDirectory,
                                                 ThreedObjectControlEditor.FilterMeshRendererTypes filterMeshRendererTypes = ThreedObjectControlEditor.FilterMeshRendererTypes.OnlySkinnedMeshRenderer,
                                                 ThreedObjectControlEditor.AttachColliderTypes attachColliderTypes = ThreedObjectControlEditor.AttachColliderTypes.BoxCollider)
    {
        List<string> pathes = FindAllThreedSearchDirectory(searchRootDirectory, SearchThreedObjectFileExtention.prefab);
        Dictionary<GameObject, KeyValuePair<Vector3, Vector3>> objectMinMaxPositions = new Dictionary<GameObject, KeyValuePair<Vector3, Vector3>>();
        for (int i = 0; i < pathes.Count; ++i)
        {
            string path = pathes[i];
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;
            List<Renderer> renderers = LoadMeshRenderers(go, filterMeshRendererTypes);
            if (renderers == null || renderers.Count <= 0) continue;
            Vector3 minimumPosition = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 maximumPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            bool isAttachCollider = false;
            for (int j = 0; j < renderers.Count; ++j)
            {
                isAttachCollider = true;

                Bounds bounds = renderers[j].bounds;
                if (minimumPosition.x > bounds.center.x - bounds.extents.x)
                {
                    minimumPosition.x = bounds.center.x - bounds.extents.x;
                }
                if (minimumPosition.y > bounds.center.y - bounds.extents.y)
                {
                    minimumPosition.y = bounds.center.y - bounds.extents.y;
                }
                if (minimumPosition.z > bounds.center.z - bounds.extents.z)
                {
                    minimumPosition.z = bounds.center.z - bounds.extents.z;
                }
                if (maximumPosition.x < bounds.center.x + bounds.extents.x)
                {
                    maximumPosition.x = bounds.center.x + bounds.extents.x;
                }
                if (maximumPosition.y < bounds.center.y + bounds.extents.y)
                {
                    maximumPosition.y = bounds.center.y + bounds.extents.y;
                }
                if (maximumPosition.z < bounds.center.z + bounds.extents.z)
                {
                    maximumPosition.z = bounds.center.z + bounds.extents.z;
                }
            }
            if (isAttachCollider)
            {
                objectMinMaxPositions.Add(go, new KeyValuePair<Vector3, Vector3>(minimumPosition, maximumPosition));
            }
        }

        AssetDatabase.StartAssetEditing();
        foreach (KeyValuePair<GameObject, KeyValuePair<Vector3, Vector3>> objectMinMaxPosition in objectMinMaxPositions)
        {
            GameObject go = objectMinMaxPosition.Key;
            KeyValuePair<Vector3, Vector3> minmaxPosition = objectMinMaxPosition.Value;
            if (attachColliderTypes == ThreedObjectControlEditor.AttachColliderTypes.BoxCollider)
            {
                BoxCollider boxCollider = go.GetComponent<BoxCollider>();
                if (boxCollider.Equals(null))
                {
                    boxCollider = go.AddComponent<BoxCollider>();
                }
                boxCollider.center = (minmaxPosition.Value + minmaxPosition.Key) / 2;
                boxCollider.size = (minmaxPosition.Value - minmaxPosition.Key);
            }
            else if (attachColliderTypes == ThreedObjectControlEditor.AttachColliderTypes.CapsuleCollider)
            {
                CapsuleCollider capsuleCollider = go.GetComponent<CapsuleCollider>();
                if (capsuleCollider.Equals(null))
                {
                    capsuleCollider = go.AddComponent<CapsuleCollider>();
                }
                capsuleCollider.center = (minmaxPosition.Value + minmaxPosition.Key) / 2;
                capsuleCollider.height = Mathf.Abs(minmaxPosition.Value.y - minmaxPosition.Key.y);
                Vector3 diff = minmaxPosition.Value - minmaxPosition.Key;
                capsuleCollider.radius = Mathf.Max(Mathf.Max(Mathf.Abs(diff.x / 2), Mathf.Abs(diff.y / 2)), Mathf.Abs(diff.z / 2));
            }
            else if (attachColliderTypes == ThreedObjectControlEditor.AttachColliderTypes.SphereCollider)
            {
                SphereCollider sphereCollider = go.GetComponent<SphereCollider>();
                if (sphereCollider.Equals(null))
                {
                    sphereCollider = go.AddComponent<SphereCollider>();
                }
                sphereCollider.center = (minmaxPosition.Value + minmaxPosition.Key) / 2;
                Vector3 diff = minmaxPosition.Value - minmaxPosition.Key;
                sphereCollider.radius = Mathf.Max(Mathf.Abs(diff.x / 2), Mathf.Abs(diff.z / 2));
            }
            else if (attachColliderTypes == ThreedObjectControlEditor.AttachColliderTypes.MeshCollider)
            {
                MeshCollider meshCollider = go.GetComponent<MeshCollider>();
                if (meshCollider.Equals(null))
                {
                    meshCollider = go.AddComponent<MeshCollider>();
                }
            }
        }
        AssetDatabase.StopAssetEditing();
        foreach (KeyValuePair<GameObject, KeyValuePair<Vector3, Vector3>> objectMinMaxPosition in objectMinMaxPositions)
        {
            EditorUtility.SetDirty(objectMinMaxPosition.Key);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Attach and edit the widdest colliders:" + objectMinMaxPositions.Count);
    }

    private static List<Renderer> LoadMeshRenderers(GameObject go, ThreedObjectControlEditor.FilterMeshRendererTypes filterMeshRendererTypes = ThreedObjectControlEditor.FilterMeshRendererTypes.OnlySkinnedMeshRenderer)
    {
        List<Renderer> renderers = new List<Renderer>();
        if (filterMeshRendererTypes == ThreedObjectControlEditor.FilterMeshRendererTypes.OnlySkinnedMeshRenderer || filterMeshRendererTypes == ThreedObjectControlEditor.FilterMeshRendererTypes.BothMeshRenderer)
        {
            List<SkinnedMeshRenderer> skinnedMeshRenderers = FindAllCompomentInChildren<SkinnedMeshRenderer>(go.transform);
            for (int j = 0; j < skinnedMeshRenderers.Count; ++j)
            {
                renderers.Add(skinnedMeshRenderers[j]);
            }
        }
        if (filterMeshRendererTypes == ThreedObjectControlEditor.FilterMeshRendererTypes.OnlyMeshRenderer || filterMeshRendererTypes == ThreedObjectControlEditor.FilterMeshRendererTypes.BothMeshRenderer)
        {
            List<MeshRenderer> meshRenderers = FindAllCompomentInChildren<MeshRenderer>(go.transform);
            for (int j = 0; j < meshRenderers.Count; ++j)
            {
                renderers.Add(meshRenderers[j]);
            }
        }
        return renderers;
    }

    public static UnityEngine.Object LoadOrCreateDB(string dbFilePath, Type type)
    {
        UnityEngine.Object db = AssetDatabase.LoadAssetAtPath(dbFilePath, type);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(db, dbFilePath);
        }
        return db;
    }

    public static T LoadOrCreateDB<T>(string dbFilePath) where T : ScriptableObject
    {
        T db = AssetDatabase.LoadAssetAtPath(dbFilePath, typeof(T)) as T;
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(db, dbFilePath);
        }
        return db;
    }

    public static List<string> FindAllThreedSearchDirectory(string searchRootDirectory, SearchThreedObjectFileExtention searchFileExtention = SearchThreedObjectFileExtention.fbx)
    {
        string extension = searchFileExtention.ToString();
        if (searchFileExtention == SearchThreedObjectFileExtention.threeds)
        {
            extension = "3ds";
        }
        return FindAllThreedSearchDirectory(searchRootDirectory, extension);
    }

    public static List<string> FindAllThreedSearchDirectory(string searchRootDirectory, string extension)
    {
        List<string> seachedFilePathes = new List<string>();
        string[] pathes = AssetDatabase.GetAllAssetPaths();
        for (int i = 0; i < pathes.Length; ++i)
        {
            string path = pathes[i];
            Match match = Regex.Match(path.ToLower(), @"" + searchRootDirectory.ToLower() + ".+." + extension);
            if (match.Success)
            {
                seachedFilePathes.Add(path);
            }
        }
        return seachedFilePathes;
    }

    private static string SetupAndGetPlaneFilePath(string exportDirectoryPath, string targetFilePath, bool distoributeParentFlag = false, int hierarchyNumber = 1)
    {
        string[] splitPath = targetFilePath.Split("/".ToCharArray());
        string filename = splitPath.Last();
        string plainFilename = filename.Split(".".ToCharArray()).First();

        string exportRootFilePath = exportDirectoryPath;
        if (distoributeParentFlag && splitPath.Length > hierarchyNumber)
        {
            exportRootFilePath += splitPath[splitPath.Length - (hierarchyNumber + 1)] + "/";
        }
        CheckAndCreateDirectory(exportRootFilePath);
        return exportRootFilePath + plainFilename;
    }

    private static void CheckAndCreateDirectory(string exportRootFilePath)
    {
        string filePath = "";
        string[] exportPathCells = exportRootFilePath.Split("/".ToCharArray());
        for (int i = 0; i < exportPathCells.Length; ++i)
        {
            if (string.IsNullOrEmpty(exportPathCells[i])) continue;

            filePath += exportPathCells[i] + "/";
            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
        }
    }

    private static T CopyAssetFile<T>(string filePath, T originAsset) where T : UnityEngine.Object
    {
        T copyAssetFile = UnityEngine.Object.Instantiate<T>(originAsset);
        AssetDatabase.CreateAsset(copyAssetFile, filePath);
        AssetDatabase.Refresh();
        return copyAssetFile;
    }

    private static List<T> FindAllCompomentInChildren<T>(Transform root) where T : class
    {
        List<T> compoments = new List<T>();
        for (int i = 0; i < root.childCount; ++i)
        {
            Transform t = root.GetChild(i);
            T compoment = t.GetComponent<T>();
            if (!compoment.Equals(null))
            {
                compoments.Add(compoment);
            }

            // It seems that null of GetCompoment differs from return null ...
            List<T> childCompoments = FindAllCompomentInChildren<T>(t);
            compoments.AddRange(childCompoments);
        }

        return compoments;
    }
}