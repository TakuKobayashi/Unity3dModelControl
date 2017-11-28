using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class FBXControlEditor
{
    private const string PrefabDirectoryPath = "Assets/Prefabs/";

    [MenuItem("Tools/ConvertFBXToPrefab")]
    public static void ConvertToFBXtoPrefab()
    {
        string[] pathes = AssetDatabase.GetAllAssetPaths();

        Dictionary<string, GameObject> pathToFBXes = new Dictionary<string, GameObject>();

        for (int i = 0; i < pathes.Length; ++i)
        {
            string path = pathes[i];
            Match match = Regex.Match(path.ToLower(), @"" + PrefabDirectoryPath.ToLower() + ".+.fbx");
            if (match.Success)
            {
                GameObject fbxObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (fbxObject == null) continue;
                pathToFBXes.Add(path, fbxObject);
            }
        }

        List<GameObject> generatedPrefabs = new List<GameObject>();

        AssetDatabase.StartAssetEditing();
        foreach (KeyValuePair<string, GameObject> pathFBX in pathToFBXes)
        {
            string path = pathFBX.Key;
            string[] splitPath = path.Split("/".ToCharArray());
            string filename = splitPath.Last();
            string plainFilename = filename.Split(".".ToCharArray()).First();
            string prefabFilePath = "";
            for (int i = 0; i < splitPath.Length - 1;++i){
                prefabFilePath += splitPath[i] + "/";
            }
            prefabFilePath += plainFilename + ".prefab";
            if (File.Exists(prefabFilePath)) continue;
            GameObject generatedPrefab = PrefabUtility.CreatePrefab(prefabFilePath, pathFBX.Value);
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
        Debug.Log("Convert FBX To Prefab Count:" + generatedPrefabs.Count);
    }

    [MenuItem("Tools/DissociateAnimationClip")]
    public static void DissociateAnimationClip()
    {
        string[] pathes = AssetDatabase.GetAllAssetPaths();

        Dictionary<string, List<AnimationClip>> pathToAnimationClips = new Dictionary<string, List<AnimationClip>>();

        for (int i = 0; i < pathes.Length; ++i)
        {
            string path = pathes[i];
            Match match = Regex.Match(path.ToLower(), @"" + PrefabDirectoryPath.ToLower() + ".+.fbx");
            if (match.Success)
            {
                List<AnimationClip> animationClips = new List<AnimationClip>();
                object[] fbxObjects = AssetDatabase.LoadAllAssetsAtPath(path);
                if (fbxObjects == null) continue;
                for (int j = 0; j < fbxObjects.Length;++j){
                    if(fbxObjects[j] is AnimationClip){
                        AnimationClip clip = fbxObjects[j] as AnimationClip;
                        if(clip.name.StartsWith("__preview__")){
                            continue;
                        }
                        animationClips.Add(clip);
                    }
                }
                if (animationClips.Count <= 0) continue;
                pathToAnimationClips.Add(path, animationClips);
            }
        }

        List<AnimationClip> generatedClips = new List<AnimationClip>();
        foreach (KeyValuePair<string, List<AnimationClip>> pathClips in pathToAnimationClips)
        {
            string path = pathClips.Key;
            string[] splitPath = path.Split("/".ToCharArray());
            string filename = splitPath.Last();
            string plainFilename = filename.Split(".".ToCharArray()).First();
            string rootDirectoryPath = "";
            for (int i = 0; i < splitPath.Length - 1; ++i)
            {
                rootDirectoryPath += splitPath[i] + "/";
            }
            for (int i = 0; i < pathClips.Value.Count;++i){
                string animFileName = rootDirectoryPath + plainFilename;
                if(i > 0){
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

    [MenuItem("Tools/AdjustBoxColliderMaxVolumnFromSkinMesh")]
    public static void AdjustBoxColliderMaxVolumnFromSkinMesh()
    {
        string[] pathes = AssetDatabase.GetAllAssetPaths();

        Dictionary<BoxCollider, SkinnedMeshRenderer> colliderMeshDic = new Dictionary<BoxCollider, SkinnedMeshRenderer>();

        for (int i = 0; i < pathes.Length; ++i)
        {
            string path = pathes[i];
            Match match = Regex.Match(path, @"" + PrefabDirectoryPath);
            if (match.Success)
            {
                GameObject skinnedObj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (skinnedObj == null) continue;
                BoxCollider boxCollider = skinnedObj.GetComponent<BoxCollider>();
                if (boxCollider.Equals(null)) continue;
                List<SkinnedMeshRenderer> skinMeshRenderers = Util.FindAllCompomentInChildren<SkinnedMeshRenderer>(skinnedObj.transform);
                if (skinMeshRenderers == null || skinMeshRenderers.Count <= 0) continue;
                float maxVolumn = float.MinValue;
                int targetIndex = -1;
                for (int j = 0; j < skinMeshRenderers.Count; ++j)
                {
                    Vector3 cubeSize = skinMeshRenderers[j].bounds.extents * 2;
                    if (maxVolumn < (cubeSize.x * cubeSize.y * cubeSize.z))
                    {
                        maxVolumn = (cubeSize.x * cubeSize.y * cubeSize.z);
                        targetIndex = j;
                    }
                }
                if (targetIndex < 0) continue;
                colliderMeshDic.Add(boxCollider, skinMeshRenderers[targetIndex]);
            }
        }
        AssetDatabase.StartAssetEditing();
        foreach(KeyValuePair<BoxCollider, SkinnedMeshRenderer> colliderMesh in colliderMeshDic)
        {
            Bounds bounds = colliderMesh.Value.bounds;
            BoxCollider boxCollider = colliderMesh.Key;
            boxCollider.center = bounds.center;
            boxCollider.size = bounds.extents * 2;
        }

        AssetDatabase.StopAssetEditing();
        //変更をUnityEditorに伝える//
        foreach (KeyValuePair<BoxCollider, SkinnedMeshRenderer> colliderMesh in colliderMeshDic)
        {
            EditorUtility.SetDirty(colliderMesh.Key);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Edit Colliders:" + colliderMeshDic.Count);
    }

    [MenuItem("Tools/AdjustBoxColliderWiddestSizeFromSkinMesh")]
    public static void AdjustBoxColliderWiddestSizeFromSkinMesh()
    {
        string[] pathes = AssetDatabase.GetAllAssetPaths();

        Dictionary<BoxCollider, KeyValuePair<Vector3, Vector3>> colliderMinMaxPositions = new Dictionary<BoxCollider, KeyValuePair<Vector3, Vector3>>();

        for (int i = 0; i < pathes.Length; ++i)
        {
            string path = pathes[i];
            Match match = Regex.Match(path, @"" + PrefabDirectoryPath);
            if (match.Success)
            {
                GameObject skinnedObj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (skinnedObj == null) continue;
                BoxCollider boxCollider = skinnedObj.GetComponent<BoxCollider>();
                if (boxCollider.Equals(null)) continue;
                List<SkinnedMeshRenderer> skinMeshRenderers = Util.FindAllCompomentInChildren<SkinnedMeshRenderer>(skinnedObj.transform);
                if (skinMeshRenderers == null || skinMeshRenderers.Count <= 0) continue;
                Vector3 minimumPosition = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 maximumPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                for (int j = 0; j < skinMeshRenderers.Count; ++j)
                {
                    Bounds bounds = skinMeshRenderers[j].bounds;
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
                colliderMinMaxPositions.Add(boxCollider, new KeyValuePair<Vector3, Vector3>(minimumPosition, maximumPosition));
            }
        }
        AssetDatabase.StartAssetEditing();
        foreach(KeyValuePair<BoxCollider, KeyValuePair<Vector3, Vector3>> colliderMinMaxPosition in colliderMinMaxPositions)
        {
            BoxCollider boxCollider = colliderMinMaxPosition.Key;
            KeyValuePair<Vector3, Vector3> minmaxPosition = colliderMinMaxPosition.Value;
            boxCollider.center = (minmaxPosition.Value + minmaxPosition.Key) / 2;
            boxCollider.size = (minmaxPosition.Value - minmaxPosition.Key);
        }

        AssetDatabase.StopAssetEditing();
        foreach (KeyValuePair<BoxCollider, KeyValuePair<Vector3, Vector3>> colliderMinMaxPosition in colliderMinMaxPositions)
        {
            EditorUtility.SetDirty(colliderMinMaxPosition.Key);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Edit Colliders:" + colliderMinMaxPositions.Count);
    }
}
