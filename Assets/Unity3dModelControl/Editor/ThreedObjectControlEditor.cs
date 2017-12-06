using UnityEngine;
using UnityEditor;
using System.Collections;
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

    public static void ConvertToPrefab(string searchRootDirectory, string exportDirectoryPath, SearchThreedObjectFileExtention searchFileExtention = SearchThreedObjectFileExtention.fbx, bool distoributeParentFlag = false, int hierarchyNumber = 1)
    {
        List<string> pathes = FindAllThreedSearchDirectory(searchRootDirectory, searchFileExtention);

        Dictionary<string, GameObject> pathToThreedObjects = new Dictionary<string, GameObject>();

        for (int i = 0; i < pathes.Count; ++i)
        {
            string path = pathes[i];
            GameObject threedObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (threedObject == null) continue;
            pathToThreedObjects.Add(path, threedObject);
        }

        List<GameObject> generatedPrefabs = new List<GameObject>();

        AssetDatabase.StartAssetEditing();
        foreach (KeyValuePair<string, GameObject> pathThreedObject in pathToThreedObjects)
        {
            string prefabFilePath = SetupAndGetPlaneFilePath(exportDirectoryPath, pathThreedObject.Key, distoributeParentFlag, hierarchyNumber) + ".prefab";
            if (File.Exists(prefabFilePath)) continue;
            GameObject generatedPrefab = PrefabUtility.CreatePrefab(prefabFilePath, pathThreedObject.Value);
            generatedPrefabs.Add(generatedPrefab);
        }
        AssetDatabase.StopAssetEditing();
        //変更をUnityEditorに伝える//
        for (int i = 0; i < generatedPrefabs.Count; ++i)
        {
            EditorUtility.SetDirty(generatedPrefabs[i]);
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
                // AnimationClipをコピーして出力(ユニークなuuid)
                AnimationClip copyClip = Object.Instantiate(pathClips.Value[i]) as AnimationClip;
                AssetDatabase.CreateAsset(copyClip, animFileName + ".tmp");
                // AnimationClipのコピー（固定化したuuid）
                File.Copy(animFileName + ".tmp", animFileName, true);
                File.Delete(animFileName + ".tmp");
                generatedClips.Add(copyClip);
            }
        }
        //変更をUnityEditorに伝える//
        for (int i = 0; i < generatedClips.Count; ++i)
        {
            EditorUtility.SetDirty(generatedClips[i]);
        }
        AssetDatabase.SaveAssets();
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
            // Instantiateして向きを調整して取りやすい位置に
            GameObject unit = GameObject.Instantiate(pathToObj.Value, Vector3.zero, Quaternion.identity) as GameObject;
            unit.transform.eulerAngles = new Vector3(270.0f, 0.0f, 0.0f);

            Vector3 nowPos = mainCamera.transform.position;
            float nowSize = mainCamera.orthographicSize;

            // カメラ調整
            mainCamera.transform.position = new Vector3(nowPos.x, nowPos.y, nowPos.z);
            mainCamera.orthographicSize = captureImageWidth;

            // RenderTextureを生成して、これに現在のSceneに映っているものを書き込む
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

            string saveFilePath = SetupAndGetPlaneFilePath(exportDirectoryPath, pathToObj.Key, distoributeParentFlag, hierarchyNumber) + "." + exportFileExtention.ToString();
            // textureのbyteをファイルに出力
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

            // 後処理
            mainCamera.targetTexture = null;
            RenderTexture.active = null;
            renderTexture.Release();

            // カメラを元に戻す
            mainCamera.transform.position = nowPos;
            mainCamera.orthographicSize = nowSize;
            Resources.UnloadUnusedAssets();
            System.GC.Collect();


            // キャプチャ撮った後は捨てる
            GameObject.DestroyImmediate(unit);
        }

        Debug.Log("Captured:" + pathToObjects.Count);
        System.GC.Collect();
    }

    public static void RegisterAssetsReference(string searchRootDirectory,
                                               string exportDirectoryPath,
                                               string exportFilePrefix = "export",
                                               bool distoributeParentFlag = false,
                                               string searchFileExtention = "prefab",
                                               int hierarchyNumber = 1,
                                               ThreedObjectControlEditor.ExportReferenceFileExtention exportFileExtention = ThreedObjectControlEditor.ExportReferenceFileExtention.asset)
    {
        List<string> pathes = FindAllThreedSearchDirectory(searchRootDirectory, searchFileExtention);
        Dictionary<GameObject, string> objectToPathes = new Dictionary<GameObject, string>();
        for (int i = 0; i < pathes.Count; ++i)
        {
            string path = pathes[i];
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;
            objectToPathes.Add(go, path);
        }
        CheckAndCreateDirectory(exportDirectoryPath);

        Dictionary<string, List<GameObject>> filePrefixObjectList = new Dictionary<string, List<GameObject>>();

        foreach (KeyValuePair<GameObject, string> objectToPath in objectToPathes)
        {
            string[] splitPath = objectToPath.Value.Split("/".ToCharArray());
            string prefixFileName = exportFilePrefix;
            if (distoributeParentFlag && splitPath.Length > hierarchyNumber)
            {
                prefixFileName = prefixFileName + splitPath[splitPath.Length - (hierarchyNumber + 1)];
            }
            string filePrefix = exportDirectoryPath + prefixFileName;
            if (!filePrefixObjectList.ContainsKey(filePrefix))
            {
                filePrefixObjectList.Add(filePrefix, new List<GameObject>());
            }
            filePrefixObjectList[filePrefix].Add(objectToPath.Key);
        }

        foreach (KeyValuePair<string, List<GameObject>> filePrefixObject in filePrefixObjectList)
        {
            filePrefixObject.Value.Sort((a, b) => string.Compare(a.name, b.name));
        }

        if (exportFileExtention == ThreedObjectControlEditor.ExportReferenceFileExtention.asset)
        {
            List<ScriptableGameObject> dbList = new List<ScriptableGameObject>();
            AssetDatabase.StartAssetEditing();
            foreach (KeyValuePair<string, List<GameObject>> filePrefixObject in filePrefixObjectList)
            {
                ScriptableGameObject db = LoadOrCreateDB<ScriptableGameObject>(filePrefixObject.Key + ".asset");
                db.gameObjects = filePrefixObject.Value.ToArray();
                dbList.Add(db);
            }
            AssetDatabase.StopAssetEditing();
            //変更をUnityEditorに伝える//
            for (int i = 0; i < dbList.Count; ++i)
            {
                EditorUtility.SetDirty(dbList[i]);
            }
        }
        else if (exportFileExtention == ThreedObjectControlEditor.ExportReferenceFileExtention.csv || exportFileExtention == ThreedObjectControlEditor.ExportReferenceFileExtention.json)
        {
            foreach (KeyValuePair<string, List<GameObject>> filePrefixObject in filePrefixObjectList)
            {
                List<string> filePathes = new List<string>();
                List<GameObject> gameObjectList = filePrefixObject.Value.ToList();
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

    private static T LoadOrCreateDB<T>(string dbFilePath) where T : ScriptableObject
    {
        T db = AssetDatabase.LoadAssetAtPath(dbFilePath, typeof(T)) as T;
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(db, dbFilePath);
        }
        return db;
    }

    private static List<string> FindAllThreedSearchDirectory(string searchRootDirectory, SearchThreedObjectFileExtention searchFileExtention = SearchThreedObjectFileExtention.fbx)
    {
        string extension = searchFileExtention.ToString();
        if (searchFileExtention == SearchThreedObjectFileExtention.threeds)
        {
            extension = "3ds";
        }
        return FindAllThreedSearchDirectory(searchRootDirectory, extension);
    }

    private static List<string> FindAllThreedSearchDirectory(string searchRootDirectory, string extension)
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
}
