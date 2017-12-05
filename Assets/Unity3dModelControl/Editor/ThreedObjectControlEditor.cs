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
        // Tell the changes to UnityEditor //
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
            for (int i = 0; i < pathClips.Value.Count;++i){
                string animFileName = animRootFileNamePath;
                if(i > 0){
                    animFileName += i.ToString();
                }
                animFileName += ".anim";
                if (File.Exists(animFileName)) continue;
                // Copy the AnimationClip and output (unique uuid)
                AnimationClip copyClip = Object.Instantiate(pathClips.Value[i]) as AnimationClip;
                AssetDatabase.CreateAsset(copyClip, animFileName + ".tmp");
                // Copy of AnimationClip (immobilized uuid)
                File.Copy(animFileName + ".tmp", animFileName, true);
                File.Delete(animFileName + ".tmp");
                generatedClips.Add(copyClip);
            }
        }
        // Tell the changes to UnityEditor //
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
            // Instantiate and adjust the orientation to a position that is easy to take
            GameObject unit = GameObject.Instantiate(pathToObj.Value, Vector3.zero, Quaternion.identity) as GameObject;
            unit.transform.eulerAngles = new Vector3(270.0f, 0.0f, 0.0f);

            Vector3 nowPos = mainCamera.transform.position;
            float nowSize = mainCamera.orthographicSize;

            // Adjust camera
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

            string saveFilePath = SetupAndGetPlaneFilePath(exportDirectoryPath, pathToObj.Key, distoributeParentFlag, hierarchyNumber)+ "." + exportFileExtention.ToString();
            // Output texture byte to file
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

            // Post processing
            mainCamera.targetTexture = null;
            RenderTexture.active = null;
            renderTexture.Release();

            // Return the camera
            mainCamera.transform.position = nowPos;
            mainCamera.orthographicSize = nowSize;
            Resources.UnloadUnusedAssets();
            System.GC.Collect();


            // Capture capture after throwing away
            GameObject.DestroyImmediate(unit);
        }

        Debug.Log("Captured:" + pathToObjects.Count);
        System.GC.Collect();
    }

    private static List<string> FindAllThreedSearchDirectory(string searchRootDirectory, SearchThreedObjectFileExtention searchFileExtention = SearchThreedObjectFileExtention.fbx)
    {
        List<string> seachedFilePathes = new List<string>();
        string[] pathes = AssetDatabase.GetAllAssetPaths();
        for (int i = 0; i < pathes.Length; ++i)
        {
            string path = pathes[i];
            string extension = searchFileExtention.ToString();
            if (searchFileExtention == SearchThreedObjectFileExtention.threeds)
            {
                extension = "3ds";
            }

            Match match = Regex.Match(path.ToLower(), @"" + searchRootDirectory.ToLower() + ".+." + extension);
            if (match.Success)
            {
                seachedFilePathes.Add(path);
            }
        }
        return seachedFilePathes;
    }

    private static string SetupAndGetPlaneFilePath(string exportDirectoryPath, string targetFilePath, bool distoributeParentFlag = false, int hierarchyNumber = 1){
        string[] splitPath = targetFilePath.Split("/".ToCharArray());
        string filename = splitPath.Last();
        string plainFilename = filename.Split(".".ToCharArray()).First();

        string exportRootFilePath = exportDirectoryPath;
        if (distoributeParentFlag && splitPath.Length > hierarchyNumber)
        {
            exportRootFilePath += splitPath[splitPath.Length - (hierarchyNumber + 1)] + "/";
        }

        string filePath = "";
        string[] exportPathCells = exportRootFilePath.Split("/".ToCharArray());
        for (int i = 0; i < exportPathCells.Length;++i){
            if (string.IsNullOrEmpty(exportPathCells[i])) continue;

            filePath += exportPathCells[i] + "/";
            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
        }

        return exportRootFilePath + plainFilename;
    }
}
