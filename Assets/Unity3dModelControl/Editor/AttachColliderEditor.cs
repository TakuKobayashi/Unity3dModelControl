using UnityEngine;
using UnityEditor;

namespace Unity3dModelControl
{
    public class AttachColliderEditor : EditorWindow
    {
        private enum Mode
        {
            MaxVolumn,
            WiddestSize
        }

        private AttachColliderEditor.Mode attachMode = AttachColliderEditor.Mode.MaxVolumn;
        private ThreedObjectControlEditor.FilterMeshRendererTypes filterMeshRendererTypes = ThreedObjectControlEditor.FilterMeshRendererTypes.OnlySkinnedMeshRenderer;
        private ThreedObjectControlEditor.AttachColliderTypes attachColliderTypes = ThreedObjectControlEditor.AttachColliderTypes.BoxCollider;

        private string searchRootDirectory = "Assets/Unity3dModelControl/Prefabs/";

        [MenuItem("Tools/AttachColliderEditor")]
        static void ShowSettingWindow()
        {
            EditorWindow.GetWindow(typeof(AttachColliderEditor));
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Attach Mode");
            AttachColliderEditor.Mode currentAttachMode = (AttachColliderEditor.Mode)EditorGUILayout.EnumPopup((AttachColliderEditor.Mode)PlayerPrefs.GetInt("AttachColliderEditor_Attach_Mode", (int)AttachColliderEditor.Mode.MaxVolumn));
            if (currentAttachMode != attachMode)
            {
                attachMode = currentAttachMode;
                PlayerPrefs.SetInt("AttachColliderEditor_Attach_Mode", (int)attachMode);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Filter MeshRenderer Type");
            filterMeshRendererTypes = (ThreedObjectControlEditor.FilterMeshRendererTypes)EditorGUILayout.EnumPopup((ThreedObjectControlEditor.FilterMeshRendererTypes)PlayerPrefs.GetInt("AttachColliderEditor_Filter_MeshRenderer_Type", (int)filterMeshRendererTypes));
            PlayerPrefs.SetInt("AttachColliderEditor_Filter_MeshRenderer_Type", (int)filterMeshRendererTypes);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Attach Collider Type");
            attachColliderTypes = (ThreedObjectControlEditor.AttachColliderTypes)EditorGUILayout.EnumPopup((ThreedObjectControlEditor.AttachColliderTypes)PlayerPrefs.GetInt("AttachColliderEditor_Attach_Collider_Type", (int)attachColliderTypes));
            PlayerPrefs.SetInt("AttachColliderEditor_Attach_Collider_Type", (int)attachColliderTypes);
            GUILayout.EndHorizontal();

            // Unity EditorのUI
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search Root Directory");
            searchRootDirectory = (string)EditorGUILayout.TextField(PlayerPrefs.GetString("AttachColliderEditor_Search_Root_Directory", searchRootDirectory));
            PlayerPrefs.SetString("AttachColliderEditor_Search_Root_Directory", searchRootDirectory);
            GUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Execute")))
            {
                if (string.IsNullOrEmpty(searchRootDirectory)) return;

                if (attachMode == AttachColliderEditor.Mode.MaxVolumn)
                {
                    ThreedObjectControlEditor.AttachColliderMaxVolumn(searchRootDirectory, filterMeshRendererTypes: filterMeshRendererTypes, attachColliderTypes: attachColliderTypes);
                }
                else if (attachMode == AttachColliderEditor.Mode.WiddestSize)
                {
                    ThreedObjectControlEditor.AttachColliderWiddestSize(searchRootDirectory, filterMeshRendererTypes: filterMeshRendererTypes, attachColliderTypes: attachColliderTypes);
                }

            }
            GUILayout.EndHorizontal();
        }
    }
}