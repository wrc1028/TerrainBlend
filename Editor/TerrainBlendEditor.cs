using UnityEngine;
using UnityEditor;

namespace TerrainBlend16
{
    [CustomEditor(typeof(TerrainBlend)), CanEditMultipleObjects]
    public class TerrainBlendEditor : Editor 
    {
        public override void OnInspectorGUI() 
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Update"))
            {
                (target as TerrainBlend).UpdateTerrainBlend();
            }
        }
    }
}