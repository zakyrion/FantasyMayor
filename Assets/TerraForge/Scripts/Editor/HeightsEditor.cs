using UnityEngine;
using UnityEditor;
using Assets.Scripts.MapGenerator.Generators;
using Assets.Scripts.MapGenerator.Maps;

namespace Assets.Scripts.MapGenerator.Generators
{
    [CustomEditor(typeof(HeightMapsGenerator))]
    class HeightsEditor : Editor
    {
        private HeightMapsGenerator generator;

        private void OnEnable()
        {
            generator = (HeightMapsGenerator)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
        }
    }
}