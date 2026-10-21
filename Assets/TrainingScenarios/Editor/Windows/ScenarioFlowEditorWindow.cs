using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Configuration.Profiles;
using TrainingScenarios.Runtime.Execution.Modes.Models;

namespace TrainingScenarios.Editor.Windows
{
    public sealed class ScenarioFlowEditorWindow : EditorWindow
    {
        private const string LastScenarioGuidKey =
            "TrainingScenarios.ScenarioFlowEditor.LastScenarioGuid";

        private static readonly List<string> ModeNames = new List<string>
        {
            "Training",
            "Exam"
        };

        [SerializeField]
        private ScenarioDefinition _scenario;
        [SerializeField]
        private ScenarioMode _mode = ScenarioMode.Training;

        private readonly List<StepDefinition> _orderedSteps =
            new List<StepDefinition>();

        private ObjectField _scenarioField;
        private DropdownField _modeField;
        private Label _heading;
        private VisualElement _messages;
        private ListView _stepList;
        private Button _applyButton;
        private Button _advancedButton;
        private bool _requiresConversion;
        private int _unassignedStepCount;

        [MenuItem("Tools/Training Scenarios/Simple Flow Editor")]
        public static void Open()
        {
            var window = GetWindow<ScenarioFlowEditorWindow>();
            window.titleContent = new GUIContent("Simple Flow");
            window.minSize = new Vector2(440f, 360f);
            window.Show();
        }

        public static void Open(ScenarioDefinition scenario)
        {
            Open();
            var window = GetWindow<ScenarioFlowEditorWindow>();
            window.SetScenario(scenario);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Simple Flow");
            minSize = new Vector2(440f, 360f);
            Undo.undoRedoPerformed += HandleUndoRedo;

            if (!_scenario && Selection.activeObject is ScenarioDefinition selected)
            {
                _scenario = selected;
            }
            else if (!_scenario)
            {
                _scenario = LoadLastScenario();
            }
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedo;
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();
            root.style.paddingLeft = 10f;
            root.style.paddingRight = 10f;
            root.style.paddingTop = 8f;
            root.style.paddingBottom = 8f;

            BuildHeader(root);
            BuildStepList(root);
            BuildFooter(root);
            ReloadFlow();
        }

        private void OnFocus()
        {
            if (_stepList != null)
            {
                ReloadFlow();
            }
        }

        private void BuildHeader(VisualElement root)
        {
            _heading = new Label("Scenario flow");
            _heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            _heading.style.fontSize = 16f;
            _heading.style.marginBottom = 8f;
            root.Add(_heading);

            var selectors = new VisualElement();
            selectors.style.flexDirection = FlexDirection.Row;
            selectors.style.alignItems = Align.Center;
            selectors.style.marginBottom = 8f;

            _scenarioField = new ObjectField("Scenario")
            {
                objectType = typeof(ScenarioDefinition),
                allowSceneObjects = false,
                value = _scenario
            };
            _scenarioField.style.flexGrow = 1f;
            _scenarioField.RegisterValueChangedCallback(evt =>
                SetScenario(evt.newValue as ScenarioDefinition));
            selectors.Add(_scenarioField);

            _modeField = new DropdownField("Mode", ModeNames, ModeToIndex(_mode));
            _modeField.style.width = 180f;
            _modeField.style.marginLeft = 8f;
            _modeField.RegisterValueChangedCallback(evt =>
            {
                _mode = evt.newValue == ModeNames[1]
                    ? ScenarioMode.Exam
                    : ScenarioMode.Training;
                ReloadFlow();
            });
            selectors.Add(_modeField);
            root.Add(selectors);

            _messages = new VisualElement();
            _messages.style.marginBottom = 6f;
            root.Add(_messages);
        }

        private void BuildStepList(VisualElement root)
        {
            _stepList = new ListView
            {
                name = "scenario-flow-step-list",
                itemsSource = _orderedSteps,
                fixedItemHeight = 58f,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                selectionType = SelectionType.Single,
                showBorder = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                showAddRemoveFooter = false,
                makeItem = MakeStepRow,
                bindItem = BindStepRow
            };
            _stepList.style.flexGrow = 1f;
            _stepList.itemIndexChanged += HandleStepMoved;
            _stepList.itemsChosen += HandleItemsChosen;
            root.Add(_stepList);
        }

        private void BuildFooter(VisualElement root)
        {
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.marginTop = 8f;

            _applyButton = new Button(() => SaveSequentialFlow("Apply scenario flow"))
            {
                text = "Apply Order"
            };
            footer.Add(_applyButton);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            footer.Add(spacer);

            var refreshButton = new Button(ReloadFlow)
            {
                text = "Refresh"
            };
            footer.Add(refreshButton);

            _advancedButton = new Button(() => ScenarioEditorWindow.Open(_scenario))
            {
                text = "Open Advanced Editor"
            };
            _advancedButton.style.marginLeft = 6f;
            footer.Add(_advancedButton);
            root.Add(footer);
        }

        private static VisualElement MakeStepRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 6f;
            row.style.paddingRight = 8f;

            var index = new Label
            {
                name = "step-index"
            };
            index.style.width = 36f;
            index.style.unityTextAlign = TextAnchor.MiddleCenter;
            index.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(index);

            var text = new VisualElement();
            text.style.flexGrow = 1f;

            var name = new Label
            {
                name = "step-name"
            };
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            text.Add(name);

            var instruction = new Label
            {
                name = "step-instruction"
            };
            instruction.style.marginTop = 2f;
            instruction.style.opacity = 0.7f;
            instruction.style.whiteSpace = WhiteSpace.NoWrap;
            text.Add(instruction);
            row.Add(text);

            return row;
        }

        private void BindStepRow(VisualElement row, int index)
        {
            if (index < 0 || index >= _orderedSteps.Count)
            {
                return;
            }

            var step = _orderedSteps[index];
            row.Q<Label>("step-index").text = (index + 1).ToString();
            row.Q<Label>("step-name").text = step
                ? GetStepName(step)
                : "Missing step reference";
            row.Q<Label>("step-instruction").text = step
                ? GetStepSubtitle(step)
                : "Open the advanced editor to repair this entry.";
            row.tooltip = step ? AssetDatabase.GetAssetPath(step) : string.Empty;
        }

        private void ReloadFlow()
        {
            _orderedSteps.Clear();
            _requiresConversion = false;
            _unassignedStepCount = 0;

            var profile = GetProfile();
            var assigned = new HashSet<StepDefinition>();
            if (profile)
            {
                var stages = profile.ActivationPlan.Stages;
                for (var stageIndex = 0; stageIndex < stages.Count; stageIndex++)
                {
                    var stage = stages[stageIndex];
                    var stageSteps = stage?.Steps;
                    if (stageSteps == null || stageSteps.Count != 1)
                    {
                        _requiresConversion = stages.Count > 0;
                    }

                    if (stageSteps == null)
                    {
                        continue;
                    }

                    for (var stepIndex = 0; stepIndex < stageSteps.Count; stepIndex++)
                    {
                        var step = stageSteps[stepIndex];
                        if (!step)
                        {
                            continue;
                        }

                        _orderedSteps.Add(step);
                        assigned.Add(step);
                    }
                }
            }

            if (_scenario)
            {
                for (var index = 0; index < _scenario.Steps.Count; index++)
                {
                    var step = _scenario.Steps[index];
                    if (!step || assigned.Contains(step))
                    {
                        continue;
                    }

                    _orderedSteps.Add(step);
                    assigned.Add(step);
                    _unassignedStepCount++;
                }
            }

            RefreshView(profile);
        }

        private void RefreshView(ScenarioModeProfile profile)
        {
            if (_heading == null)
            {
                return;
            }

            if (_scenarioField != null && _scenarioField.value != _scenario)
            {
                _scenarioField.SetValueWithoutNotify(_scenario);
            }

            if (_modeField != null)
            {
                _modeField.SetValueWithoutNotify(ModeNames[ModeToIndex(_mode)]);
            }

            _heading.text = _scenario
                ? $"{GetScenarioName(_scenario)} — {_mode} flow"
                : "Scenario flow";

            _stepList?.RefreshItems();
            var canEdit = _scenario && profile;
            _stepList?.SetEnabled(canEdit);
            if (_stepList != null)
            {
                _stepList.reorderable = canEdit;
            }

            _applyButton?.SetEnabled(canEdit && _orderedSteps.Count > 0);
            _advancedButton?.SetEnabled(_scenario);

            RebuildMessages(profile);
        }

        private void RebuildMessages(ScenarioModeProfile profile)
        {
            _messages.Clear();
            if (!_scenario)
            {
                AddMessage("Select a Scenario Definition.", HelpBoxMessageType.Info);
                return;
            }

            if (!profile)
            {
                AddMessage(
                    $"The scenario has no {_mode} profile. Assign it in the advanced editor.",
                    HelpBoxMessageType.Error);
                return;
            }

            if (_requiresConversion)
            {
                AddMessage(
                    "Grouped stages are shown here as one linear list. Moving a step or applying the order will make this mode sequential; Undo restores the previous grouping.",
                    HelpBoxMessageType.Info);
            }
            else if (_unassignedStepCount > 0)
            {
                AddMessage(
                    $"{_unassignedStepCount} unassigned scenario step(s) are shown at the end. Apply or move them to include them in this mode.",
                    HelpBoxMessageType.Info);
            }

            if (_mode == ScenarioMode.Exam)
            {
                AddMessage(
                    "This window changes only activation order. Exam groups remain unchanged.",
                    HelpBoxMessageType.Info);
            }
        }

        private void AddMessage(string message, HelpBoxMessageType type)
        {
            var box = new HelpBox(message, type);
            box.style.marginBottom = 4f;
            _messages.Add(box);
        }

        private void HandleStepMoved(int previousIndex, int newIndex)
        {
            _stepList.RefreshItems();
            SaveSequentialFlow("Reorder scenario steps");
        }

        private static void HandleItemsChosen(IEnumerable<object> items)
        {
            foreach (var item in items)
            {
                if (!(item is StepDefinition step) || !step)
                {
                    continue;
                }

                Selection.activeObject = step;
                EditorGUIUtility.PingObject(step);
                return;
            }
        }

        private void SaveSequentialFlow(string undoName)
        {
            var profile = GetProfile();
            if (!_scenario || !profile)
            {
                return;
            }

            Undo.RecordObject(profile, undoName);
            var serializedProfile = new SerializedObject(profile);
            serializedProfile.Update();
            var stages = serializedProfile
                .FindProperty("_activationPlan")
                .FindPropertyRelative("_stages");

            stages.arraySize = _orderedSteps.Count;
            for (var index = 0; index < _orderedSteps.Count; index++)
            {
                var stage = stages.GetArrayElementAtIndex(index);
                var steps = stage.FindPropertyRelative("_steps");
                steps.arraySize = 1;
                steps.GetArrayElementAtIndex(0).objectReferenceValue = _orderedSteps[index];
            }

            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            ReloadFlow();
        }

        private ScenarioModeProfile GetProfile()
        {
            return _scenario ? _scenario.GetProfile(_mode) : null;
        }

        private void SetScenario(ScenarioDefinition scenario)
        {
            _scenario = scenario;
            SaveLastScenario(scenario);
            ReloadFlow();
        }

        private void HandleUndoRedo()
        {
            ReloadFlow();
        }

        private static int ModeToIndex(ScenarioMode mode)
        {
            return mode == ScenarioMode.Exam ? 1 : 0;
        }

        private static string GetScenarioName(ScenarioDefinition scenario)
        {
            return string.IsNullOrWhiteSpace(scenario.DisplayName)
                ? scenario.name
                : scenario.DisplayName;
        }

        private static string GetStepName(StepDefinition step)
        {
            return string.IsNullOrWhiteSpace(step.DisplayName)
                ? step.name
                : step.DisplayName;
        }

        private static string GetStepSubtitle(StepDefinition step)
        {
            if (!string.IsNullOrWhiteSpace(step.Instruction))
            {
                return step.Instruction;
            }

            return string.IsNullOrWhiteSpace(step.StepId)
                ? AssetDatabase.GetAssetPath(step)
                : step.StepId;
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
    }
}
