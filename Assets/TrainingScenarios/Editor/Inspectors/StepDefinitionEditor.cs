using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Rules.Contracts;
using TrainingScenarios.Runtime.Rules.Definitions;

namespace TrainingScenarios.Editor.Inspectors
{
    [CustomEditor(typeof(StepDefinition))]
    public sealed class StepDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty _ruleDefinition;
        private Type[] _ruleDefinitionTypes = Array.Empty<Type>();

        private void OnEnable()
        {
            _ruleDefinition = serializedObject.FindProperty("_ruleDefinition");
            _ruleDefinitionTypes = TypeCache
                .GetTypesDerivedFrom<IStepRuleDefinition>()
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    type.IsSerializable &&
                    type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_stepId"),
                new GUIContent("Stable GUID"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_displayName"),
                new GUIContent("Display Name"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_instruction"),
                new GUIContent("Instruction"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_uiHint"),
                new GUIContent("UI Hint"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Completion Rule", EditorStyles.boldLabel);
            DrawRuleSelector();
            DrawRuleConfiguration();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRuleSelector()
        {
            var ruleName = GetRuleName();
            var buttonRect = EditorGUILayout.GetControlRect();

            if (!EditorGUI.DropdownButton(
                    buttonRect,
                    new GUIContent(ruleName),
                    FocusType.Keyboard))
            {
                return;
            }

            var menu = new GenericMenu();
            var currentType = _ruleDefinition.managedReferenceValue?.GetType();
            foreach (var ruleType in _ruleDefinitionTypes)
            {
                var capturedType = ruleType;
                menu.AddItem(
                    new GUIContent(GetRuleName(capturedType)),
                    currentType == capturedType,
                    () => AssignRule(Activator.CreateInstance(capturedType)));
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(
                new GUIContent("Not Assigned"),
                _ruleDefinition.managedReferenceValue == null,
                () => AssignRule(null));
            menu.DropDown(buttonRect);
        }

        private void DrawRuleConfiguration()
        {
            switch (_ruleDefinition.managedReferenceValue)
            {
                case SingleActionRuleDefinition:
                    DrawActionRequirement(
                        _ruleDefinition.FindPropertyRelative("_expectedAction"),
                        "Expected Action");
                    DrawRelativeProperty("_violationReason", "Violation Reason");
                    break;
                case OrderedActionsRuleDefinition:
                    DrawActionRequirement(
                        _ruleDefinition.FindPropertyRelative("_firstAction"),
                        "First Action");
                    DrawActionRequirement(
                        _ruleDefinition.FindPropertyRelative("_secondAction"),
                        "Second Action");
                    DrawRelativeProperty("_orderViolationReason", "Order Violation Reason");
                    DrawRelativeProperty("_unexpectedActionReason", "Unexpected Action Reason");
                    break;
                case GuardedActionRuleDefinition:
                    DrawActionRequirement(
                        _ruleDefinition.FindPropertyRelative("_expectedAction"),
                        "Expected Action");
                    DrawRelativeProperty("_requiredStateId", "State Binding ID");
                    DrawRelativeProperty("_requiredStateValue", "Required State Value");
                    DrawRelativeProperty("_guardViolationReason", "Guard Violation Reason");
                    DrawRelativeProperty("_unexpectedActionReason", "Unexpected Action Reason");
                    break;
                default:
                    if (_ruleDefinition.managedReferenceValue == null)
                    {
                        EditorGUILayout.HelpBox(
                            "Select a completion rule for the step.",
                            MessageType.Warning);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(
                            _ruleDefinition,
                            GUIContent.none,
                            true);
                    }

                    break;
            }
        }

        private void DrawActionRequirement(SerializedProperty property, string label)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(
                    property.FindPropertyRelative("_sourceId"),
                    new GUIContent("Source ID"));
                EditorGUILayout.PropertyField(
                    property.FindPropertyRelative("_actionType"),
                    new GUIContent("Action Type"));
                EditorGUILayout.PropertyField(
                    property.FindPropertyRelative("_targetId"),
                    new GUIContent("Target ID"));
            }
        }

        private void DrawRelativeProperty(string propertyName, string label)
        {
            EditorGUILayout.PropertyField(
                _ruleDefinition.FindPropertyRelative(propertyName),
                new GUIContent(label));
        }

        private string GetRuleName()
        {
            var rule = _ruleDefinition.managedReferenceValue;
            return rule == null ? "Select Rule" : GetRuleName(rule.GetType());
        }

        private static string GetRuleName(Type type)
        {
            const string suffix = "RuleDefinition";
            var typeName = type.Name.EndsWith(suffix, StringComparison.Ordinal)
                ? type.Name.Substring(0, type.Name.Length - suffix.Length)
                : type.Name;
            return ObjectNames.NicifyVariableName(typeName);
        }

        private void AssignRule(object ruleDefinition)
        {
            serializedObject.Update();
            _ruleDefinition.managedReferenceValue = ruleDefinition;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
