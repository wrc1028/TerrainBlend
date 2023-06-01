using UnityEngine;
using UnityEditor;
namespace TerrainBlend16
{
    
    [CustomEditor(typeof(TerrainBlendAsset))]
    public class TerrainBlendAssetEditor : Editor 
    {
        public override void OnInspectorGUI() 
        {
            TerrainBlendAsset source = (TerrainBlendAsset)target;
            base.OnInspectorGUI();
            GUILayout.Space(10);
            source.m_TerrainData = (TerrainData)EditorGUILayout.ObjectField("Terrain", source.m_TerrainData, typeof(TerrainData), false);
            if (source.m_TerrainData != null && GUILayout.Button("初始化地形文件"))
            {
                source.InitTerrainBlendAsset();
                EditorUtility.SetDirty(source);
            }
            GUILayout.Space(5);
            if (source.m_TerrainData != null && GUILayout.Button("擦除"))
            {
                source.AutoErase();
                EditorUtility.SetDirty(source);
            }
            GUILayout.Space(5);
            if (source.m_TerrainData != null && GUILayout.Button("合并"))
            {
                source.CombineTextureArray();
                EditorUtility.SetDirty(source);
            }
        }
    }
}