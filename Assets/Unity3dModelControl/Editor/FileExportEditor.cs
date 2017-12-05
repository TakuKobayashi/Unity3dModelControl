using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class FileExportEditor : EditorWindow
{
    private enum Mode
    {
        ConvertToPrefab,
        DissociateAnimationClip,
        CaptureSceneImage
    }

    private FileExportEditor.Mode exportMode = FileExportEditor.Mode.ConvertToPrefab;
    private ThreedObjectControlEditor.ExportImageFileExtention imageExportFileExtention = ThreedObjectControlEditor.ExportImageFileExtention.png;
    private ThreedObjectControlEditor.SearchThreedObjectFileExtention threedObjectSearchFileExtention = ThreedObjectControlEditor.SearchThreedObjectFileExtention.fbx;

    private string searchRootDirectory = "Assets/Unity3dModelControl/Prefabs/";
    private string exportDirectoryPath = "Assets/Unity3dModelControl/Prefabs/";
    private int captureImageWidth = 128;
    private int captureImageHeight = 128;
    private int hierarchyNumber = 1;
    private bool distoributeParentFlag = false;

    [MenuItem("Tools/FileExportEditor")]
    static void ShowSettingWindow()
    {
        EditorWindow.GetWindow(typeof(FileExportEditor));
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Export Mode");
        FileExportEditor.Mode currentExportMode = (FileExportEditor.Mode) EditorGUILayout.EnumPopup((FileExportEditor.Mode) PlayerPrefs.GetInt("FileExportEditor_Export_Mode", (int) FileExportEditor.Mode.ConvertToPrefab));
        if (currentExportMode != exportMode)
        {
            exportMode = currentExportMode;
            PlayerPrefs.SetInt("FileExportEditor_Export_Mode", (int) exportMode);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Distribute with the parent directory?");
        distoributeParentFlag = EditorGUILayout.Toggle(distoributeParentFlag);
        EditorGUILayout.EndHorizontal();

        if(distoributeParentFlag){
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Refer hierarchy parent number");
            hierarchyNumber = EditorGUILayout.IntField(hierarchyNumber);
            EditorGUILayout.EndHorizontal();
        }

        if (exportMode == FileExportEditor.Mode.CaptureSceneImage)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Capture Image Width");
            captureImageWidth = EditorGUILayout.IntField(captureImageWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Capture Image Height");
            captureImageHeight = EditorGUILayout.IntField(captureImageHeight);
            GUILayout.EndHorizontal();
        }

        // Unity EditorのUI
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search Root Directory");
        searchRootDirectory = (string) EditorGUILayout.TextField(PlayerPrefs.GetString("FileExportEditor_Search_Root_Directory", searchRootDirectory));
        PlayerPrefs.SetString("FileExportEditor_Search_Root_Directory", searchRootDirectory);
        GUILayout.EndHorizontal();

        List<string> values = new List<string>();
        Array exportImageFiles = Enum.GetValues(typeof(ThreedObjectControlEditor.SearchThreedObjectFileExtention));
        for (int i = 0; i < exportImageFiles.Length; ++i)
        {
            ThreedObjectControlEditor.SearchThreedObjectFileExtention ext = (ThreedObjectControlEditor.SearchThreedObjectFileExtention)exportImageFiles.GetValue(i);
            if (ext == ThreedObjectControlEditor.SearchThreedObjectFileExtention.prefab)
            {
                continue;
            }
            values.Add(ext.ToString());
        }

        if (exportMode == FileExportEditor.Mode.ConvertToPrefab || exportMode == FileExportEditor.Mode.DissociateAnimationClip)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search File Extention");
            threedObjectSearchFileExtention = (ThreedObjectControlEditor.SearchThreedObjectFileExtention)EditorGUILayout.Popup(PlayerPrefs.GetInt("FileExportEditor_Search_File_Extention", (int) threedObjectSearchFileExtention), values.ToArray());
            PlayerPrefs.SetInt("FileExportEditor_Search_File_Extention", (int) threedObjectSearchFileExtention);
            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Export Directory");
        exportDirectoryPath = (string) EditorGUILayout.TextField(PlayerPrefs.GetString("FileExportEditor_Export_Directory", exportDirectoryPath));
        PlayerPrefs.SetString("FileExportEditor_Export_Directory", exportDirectoryPath);
        GUILayout.EndHorizontal();

        if (exportMode == FileExportEditor.Mode.CaptureSceneImage)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Export File Extention");
            imageExportFileExtention = (ThreedObjectControlEditor.ExportImageFileExtention) EditorGUILayout.EnumPopup((ThreedObjectControlEditor.ExportImageFileExtention) PlayerPrefs.GetInt("FileExportEditor_Export_File_Extention", (int)imageExportFileExtention));
            PlayerPrefs.SetInt("FileExportEditor_Export_File_Extention", (int) imageExportFileExtention);
            GUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("Execute")))
        {
            if (string.IsNullOrEmpty(searchRootDirectory)) return;

            if (exportMode == FileExportEditor.Mode.CaptureSceneImage)
            {
                ThreedObjectControlEditor.CaptureImage(searchRootDirectory, exportDirectoryPath, Camera.main, captureImageWidth, captureImageHeight, distoributeParentFlag: distoributeParentFlag, hierarchyNumber: hierarchyNumber);
            }
            else if (exportMode == FileExportEditor.Mode.ConvertToPrefab)
            {
                ThreedObjectControlEditor.ConvertToPrefab(searchRootDirectory, exportDirectoryPath, searchFileExtention: threedObjectSearchFileExtention, distoributeParentFlag: distoributeParentFlag, hierarchyNumber: hierarchyNumber);
            }
            else if (exportMode == FileExportEditor.Mode.DissociateAnimationClip)
            {
                ThreedObjectControlEditor.DissociateAnimationClip(searchRootDirectory, exportDirectoryPath, searchFileExtention: threedObjectSearchFileExtention, distoributeParentFlag: distoributeParentFlag, hierarchyNumber: hierarchyNumber);
            }
            GUILayout.EndHorizontal();
        }
    }
}
