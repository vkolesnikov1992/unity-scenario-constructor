using UnityEditor;
using UnityEngine;
using TrainingScenarios.Editor.Windows;
using TrainingScenarios.Runtime.Configuration.Definitions;

namespace TrainingScenarios.Editor.Inspectors
{
    [CustomEditor(typeof(ScenarioDefinition))]
    public sealed class ScenarioDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_scenarioId"),
                new GUIContent("Stable ID"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_displayName"),
                new GUIContent("Display Name"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_description"),
                new GUIContent("Description"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_trainingProfile"),
                new GUIContent("Training Profile"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_examProfile"),
                new GUIContent("Exam Profile"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_loggerProvider"),
                new GUIContent("Logger"));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Scenario steps, activation stages, and exam groups are edited " +
                "in the Scenario Editor window.",
                MessageType.Info);

            if (GUILayout.Button("Open Scenario Editor"))
            {
                ScenarioEditorWindow.Open((ScenarioDefinition)target);
            }
        }
    }
}
