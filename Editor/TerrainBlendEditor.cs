using UnityEngine;
using UnityEditor;

namespace TerrainBlend16
{
    [CustomEditor(typeof(TerrainBlend)), CanEditMultipleObjects]
    public class TerrainBlendEditor : Editor 
    {
        public override void OnInspectorGUI() 
        {
            TerrainBlend source = target as TerrainBlend;
            base.OnInspectorGUI();
            if (GUILayout.Button("Update"))
            {
                source.UpdateTerrainBlend();
            }
        }
    }
}