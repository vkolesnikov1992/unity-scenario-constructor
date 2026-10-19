using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Configuration.Profiles;
using TrainingScenarios.Runtime.Configuration.Providers;

namespace TrainingScenarios.Editor.Creation
{
    public static class ScenarioAssetCreator
    {
        private const string UndoName = "Create Scenario Setup";

        public static ScenarioDefinition CreateWithSaveDialog()
        {
            var selectedPath = EditorUtility.SaveFilePanelInProject(
                "Create Scenario Setup",
                "NewScenario",
                "asset",
                "Choose where to save the scenario and its supporting assets.");
            if (string.IsNullOrEmpty(selectedPath))
            {
                return null;
            }

            return Create(selectedPath);
        }

        private static ScenarioDefinition Create(string selectedPath)
        {
            var scenarioPath = AssetDatabase.GenerateUniqueAssetPath(selectedPath);
            var directory = Path.GetDirectoryName(scenarioPath).Replace('\\', '/');
            var baseName = Path.GetFileNameWithoutExtension(scenarioPath);

            var scenario = ScriptableObject.CreateInstance<ScenarioDefinition>();
            var trainingProfile = ScriptableObject.CreateInstance<TrainingModeProfile>();
            var examProfile = ScriptableObject.CreateInstance<ExamModeProfile>();
            var loggerProvider = ScriptableObject.CreateInstance<JsonLoggerProvider>();
            var trainingAnalyzer = ScriptableObject.CreateInstance<TrainingAnalyzerProvider>();
            var examAnalyzer = ScriptableObject.CreateInstance<ExamAnalyzerProvider>();

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);

            CreateAsset(scenario, scenarioPath);
            CreateAsset(
                trainingProfile,
                BuildPath(directory, $"{baseName}_Training.asset"));
            CreateAsset(
                examProfile,
                BuildPath(directory, $"{baseName}_Exam.asset"));
            CreateAsset(
                loggerProvider,
                BuildPath(directory, $"{baseName}_JsonLogger.asset"));
            CreateAsset(
                trainingAnalyzer,
                BuildPath(directory, $"{baseName}_TrainingAnalyzer.asset"));
            CreateAsset(
                examAnalyzer,
                BuildPath(directory, $"{baseName}_ExamAnalyzer.asset"));

            ConfigureProfile(trainingProfile, trainingAnalyzer);
            ConfigureProfile(examProfile, examAnalyzer);
            ConfigureScenario(
                scenario,
                baseName,
                trainingProfile,
                examProfile,
                loggerProvider);

            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);
            return scenario;
        }

        private static void CreateAsset(UnityEngine.Object asset, string path)
        {
            AssetDatabase.CreateAsset(
                asset,
                AssetDatabase.GenerateUniqueAssetPath(path));
            Undo.RegisterCreatedObjectUndo(asset, UndoName);
        }

        private static void ConfigureProfile(
            ScriptableObject profile,
            AnalyzerProvider analyzerProvider)
        {
            var serializedProfile = new SerializedObject(profile);
            serializedProfile.FindProperty("_analyzerProvider").objectReferenceValue =
                analyzerProvider;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureScenario(
            ScenarioDefinition scenario,
            string baseName,
            TrainingModeProfile trainingProfile,
            ExamModeProfile examProfile,
            LoggerProvider loggerProvider)
        {
            var serializedScenario = new SerializedObject(scenario);
            serializedScenario.FindProperty("_scenarioId").stringValue =
                Guid.NewGuid().ToString("N");
            serializedScenario.FindProperty("_displayName").stringValue =
                ObjectNames.NicifyVariableName(baseName);
            serializedScenario.FindProperty("_trainingProfile").objectReferenceValue =
                trainingProfile;
            serializedScenario.FindProperty("_examProfile").objectReferenceValue =
                examProfile;
            serializedScenario.FindProperty("_loggerProvider").objectReferenceValue =
                loggerProvider;
            serializedScenario.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string BuildPath(string directory, string fileName)
        {
            return $"{directory}/{fileName}";
        }
    }
}
