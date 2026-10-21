using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrainingScenarios.Editor.Creation;
using TrainingScenarios.Runtime.Composition;
using TrainingScenarios.Runtime.Configuration.Definitions;

namespace TrainingScenarios.Editor.Windows
{
    public sealed class ScenarioEditorWindow : EditorWindow
    {
        private const string LastScenarioGuidKey =
            "TrainingScenarios.ScenarioEditor.LastScenarioGuid";

        private static readonly string[] ModeNames =
        {
            "Training",
            "Exam"
        };

        private ScenarioDefinition _scenario;
        private SerializedObject _scenarioObject;
        private SerializedObject _trainingProfileObject;
        private SerializedObject _examProfileObject;
        private ScenarioCollectionEditor _collectionEditor;
        private ScenarioValidationPanel _validationPanel;
        private Vector2 _scrollPosition;
        private int _selectedMode;

        [MenuItem("Tools/Training Scenarios/Scenario Editor")]
        public static void Open()
        {
            var window = GetWindow<ScenarioEditorWindow>();
            window.titleContent = new GUIContent("Scenario Editor");
            window.minSize = new Vector2(640f, 520f);
            window.Show();
        }

        public static void Open(ScenarioDefinition scenario)
        {
            Open();
            var window = GetWindow<ScenarioEditorWindow>();
            window.SetScenario(scenario);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Scenario Editor");
            minSize = new Vector2(640f, 520f);
            _validationPanel = new ScenarioValidationPanel();
            _collectionEditor = new ScenarioCollectionEditor(
                HandleConfigurationChanged,
                Repaint);
            Undo.undoRedoPerformed += HandleUndoRedo;

            if (!_scenario && Selection.activeObject is ScenarioDefinition selectedScenario)
            {
                SetScenario(selectedScenario);
            }
            else if (!_scenario)
            {
                SetScenario(LoadLastScenario());
            }
            else
            {
                RebuildSerializedState();
            }
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedo;
        }

        private void OnGUI()
        {
            _collectionEditor.HandleCurrentEvent();
            DrawScenarioSelector();

            if (!_scenario)
            {
                EditorGUILayout.HelpBox(
                    "Select a Scenario Definition or create a new scenario setup.",
                    MessageType.Info);
                if (GUILayout.Button("Create Scenario Setup"))
                {
                    CreateScenarioSetup();
                }

                return;
            }

            EnsureSerializedState();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawScenarioConfiguration();
            EditorGUILayout.Space(8f);
            DrawModeConfiguration();
            EditorGUILayout.Space(8f);
            _validationPanel.Draw();
            EditorGUILayout.EndScrollView();

            DrawBottomToolbar();
        }

        private void DrawScenarioSelector()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var selectedScenario = (ScenarioDefinition)EditorGUILayout.ObjectField(
                "Scenario",
                _scenario,
                typeof(ScenarioDefinition),
                false);

            if (EditorGUI.EndChangeCheck())
            {
                SetScenario(selectedScenario);
            }

            if (GUILayout.Button("New", GUILayout.Width(64f)))
            {
                CreateScenarioSetup();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void CreateScenarioSetup()
        {
            var scenario = ScenarioAssetCreator.CreateWithSaveDialog();
            if (!scenario)
            {
                return;
            }

            Selection.activeObject = scenario;
            EditorGUIUtility.PingObject(scenario);
            SetScenario(scenario);
        }

        private void DrawScenarioConfiguration()
        {
            _scenarioObject.Update();

            EditorGUILayout.LabelField("Scenario Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _scenarioObject.FindProperty("_scenarioId"),
                new GUIContent("Stable ID"));
            EditorGUILayout.PropertyField(
                _scenarioObject.FindProperty("_displayName"),
                new GUIContent("Display Name"));
            EditorGUILayout.PropertyField(
                _scenarioObject.FindProperty("_description"),
                new GUIContent("Description"));
            EditorGUILayout.PropertyField(
                _scenarioObject.FindProperty("_trainingProfile"),
                new GUIContent("Training Profile"));
            EditorGUILayout.PropertyField(
                _scenarioObject.FindProperty("_examProfile"),
                new GUIContent("Exam Profile"));
            EditorGUILayout.PropertyField(
                _scenarioObject.FindProperty("_loggerProvider"),
                new GUIContent("Logger"));

            EditorGUILayout.Space(4f);
            _collectionEditor.DrawScenarioSteps();

            if (_scenarioObject.ApplyModifiedProperties())
            {
                HandleConfigurationChanged();
                RebuildProfileEditors();
            }
        }

        private void DrawModeConfiguration()
        {
            _selectedMode = GUILayout.Toolbar(_selectedMode, ModeNames);
            EditorGUILayout.Space(4f);

            if (_selectedMode == 0)
            {
                DrawTrainingProfile();
            }
            else
            {
                DrawExamProfile();
            }
        }

        private void DrawTrainingProfile()
        {
            if (_trainingProfileObject == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a training profile.",
                    MessageType.Error);
                return;
            }

            _trainingProfileObject.Update();
            EditorGUILayout.LabelField("Training Profile", EditorStyles.boldLabel);
            DrawInactiveActionBehavior(_trainingProfileObject);
            EditorGUILayout.PropertyField(
                _trainingProfileObject.FindProperty("_analyzerProvider"),
                new GUIContent("Analyzer"));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Scoring", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _trainingProfileObject.FindProperty("_firstAttemptScore"),
                new GUIContent("First Attempt"));
            EditorGUILayout.PropertyField(
                _trainingProfileObject.FindProperty("_secondAttemptScore"),
                new GUIContent("Second Attempt"));
            EditorGUILayout.PropertyField(
                _trainingProfileObject.FindProperty("_laterAttemptScore"),
                new GUIContent("Third and Later Attempts"));
            EditorGUILayout.PropertyField(
                _trainingProfileObject.FindProperty("_maximumPerStep"),
                new GUIContent("Maximum per Step"));

            EditorGUILayout.Space(4f);
            _collectionEditor.DrawTrainingStages();

            if (_trainingProfileObject.ApplyModifiedProperties())
            {
                HandleConfigurationChanged();
            }
        }

        private void DrawExamProfile()
        {
            if (_examProfileObject == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign an exam profile.",
                    MessageType.Error);
                return;
            }

            _examProfileObject.Update();
            EditorGUILayout.LabelField("Exam Profile", EditorStyles.boldLabel);
            DrawInactiveActionBehavior(_examProfileObject);
            EditorGUILayout.PropertyField(
                _examProfileObject.FindProperty("_analyzerProvider"),
                new GUIContent("Analyzer"));
            EditorGUILayout.PropertyField(
                _examProfileObject.FindProperty("_violationThreshold"),
                new GUIContent("Violation Threshold"));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Scoring", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _examProfileObject.FindProperty("_maximumScore"),
                new GUIContent("Maximum Score"));
            EditorGUILayout.PropertyField(
                _examProfileObject.FindProperty("_failedStepPenalty"),
                new GUIContent("Failed Step Penalty"));
            EditorGUILayout.PropertyField(
                _examProfileObject.FindProperty("_skippedStepPenalty"),
                new GUIContent("Skipped Step Penalty"));

            EditorGUILayout.Space(4f);
            _collectionEditor.DrawExamStages();
            EditorGUILayout.Space(6f);
            _collectionEditor.DrawExamGroups();

            if (_examProfileObject.ApplyModifiedProperties())
            {
                HandleConfigurationChanged();
            }
        }

        private void DrawInactiveActionBehavior(SerializedObject profileObject)
        {
            var property = profileObject.FindProperty("_inactiveActionBehavior");
            var labels = new[]
            {
                "Record Violation",
                "Ignore"
            };
            property.enumValueIndex = EditorGUILayout.Popup(
                "Inactive Action Behavior",
                property.enumValueIndex,
                labels);
        }

        private void DrawBottomToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Validate Scenario", EditorStyles.toolbarButton))
            {
                ApplyAllModifiedProperties();
                _validationPanel.Validate(_scenario);
                Repaint();
            }

            using (new EditorGUI.DisabledScope(!_scenario))
            {
                if (GUILayout.Button("Simple Flow", EditorStyles.toolbarButton))
                {
                    ApplyAllModifiedProperties();
                    ScenarioFlowEditorWindow.Open(_scenario);
                }
            }

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(
                       !_scenario ||
                       _validationPanel.HasErrors ||
                       EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Play Selected Scenario", EditorStyles.toolbarButton))
                {
                    StartSelectedScenario();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void StartSelectedScenario()
        {
            ApplyAllModifiedProperties();
            var activeScene = SceneManager.GetActiveScene();
            var contexts = Object.FindObjectsByType<ScenarioSceneContext>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(context => context.gameObject.scene == activeScene)
                .ToArray();

            if (contexts.Length != 1)
            {
                var message = contexts.Length == 0
                    ? "The active scene has no ScenarioSceneContext. Add one before starting the scenario."
                    : "The active scene has multiple ScenarioSceneContext components. Leave exactly one before starting the scenario.";
                EditorUtility.DisplayDialog("Cannot Start Scenario", message, "OK");
                return;
            }

            _validationPanel.Validate(_scenario, contexts[0].SceneBindings);
            if (_validationPanel.HasErrors)
            {
                Repaint();
                return;
            }

            var contextObject = new SerializedObject(contexts[0]);
            var scenarioProperty = contextObject.FindProperty("_scenario");
            if (scenarioProperty.objectReferenceValue != _scenario)
            {
                Undo.RecordObject(contexts[0], "Bind selected scenario to scene");
                scenarioProperty.objectReferenceValue = _scenario;
                contextObject.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(activeScene);
            }

            if (activeScene.isDirty &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorApplication.isPlaying = true;
        }

        private void ApplyAllModifiedProperties()
        {
            _scenarioObject?.ApplyModifiedProperties();
            _trainingProfileObject?.ApplyModifiedProperties();
            _examProfileObject?.ApplyModifiedProperties();
        }

        private void SetScenario(ScenarioDefinition scenario)
        {
            _scenario = scenario;
            SaveLastScenario(scenario);
            RebuildSerializedState();
            Repaint();
        }

        private static ScenarioDefinition LoadLastScenario()
        {
            var guid = EditorPrefs.GetString(LastScenarioGuidKey);
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            var scenario = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(path);
            if (!scenario)
            {
                EditorPrefs.DeleteKey(LastScenarioGuidKey);
            }

            return scenario;
        }

        private static void SaveLastScenario(ScenarioDefinition scenario)
        {
            if (!scenario)
            {
                EditorPrefs.DeleteKey(LastScenarioGuidKey);
                return;
            }

            var path = AssetDatabase.GetAssetPath(scenario);
            EditorPrefs.SetString(
                LastScenarioGuidKey,
                AssetDatabase.AssetPathToGUID(path));
        }

        private void EnsureSerializedState()
        {
            if (_scenarioObject == null || _scenarioObject.targetObject != _scenario)
            {
                RebuildSerializedState();
                return;
            }

            if (_trainingProfileObject?.targetObject != _scenario.TrainingProfile ||
                _examProfileObject?.targetObject != _scenario.ExamProfile)
            {
                RebuildProfileEditors();
            }
        }

        private void RebuildSerializedState()
        {
            _scenarioObject = !_scenario ? null : new SerializedObject(_scenario);
            RebuildProfileEditors();
            _validationPanel?.Invalidate();
        }

        private void RebuildProfileEditors()
        {
            _trainingProfileObject = !_scenario || !_scenario.TrainingProfile
                ? null
                : new SerializedObject(_scenario.TrainingProfile);
            _examProfileObject = !_scenario || !_scenario.ExamProfile
                ? null
                : new SerializedObject(_scenario.ExamProfile);
            _collectionEditor?.Rebuild(
                _scenario,
                _scenarioObject,
                _trainingProfileObject,
                _examProfileObject);
        }

        private void HandleConfigurationChanged()
        {
            _validationPanel.Invalidate();
            Repaint();
        }

        private void HandleUndoRedo()
        {
            RebuildSerializedState();
            Repaint();
        }
    }
}
