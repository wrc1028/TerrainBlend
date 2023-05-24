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
            if (source.m_TerrainData != null && GUILayout.Button("更新"))
            {
                source.InitTerrainBlendAsset();
                EditorUtility.SetDirty(source);
            }
        }
    }
}