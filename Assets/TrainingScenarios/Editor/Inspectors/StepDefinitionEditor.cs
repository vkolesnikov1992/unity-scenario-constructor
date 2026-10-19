using UnityEditor;
using UnityEngine;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Rules.Definitions;

namespace TrainingScenarios.Editor.Inspectors
{
    [CustomEditor(typeof(StepDefinition))]
    public sealed class StepDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty _ruleDefinition;

        private void OnEnable()
        {
            _ruleDefinition = serializedObject.FindProperty("_ruleDefinition");
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
            menu.AddItem(
                new GUIContent("Single Action"),
                _ruleDefinition.managedReferenceValue is SingleActionRuleDefinition,
                () => AssignRule(new SingleActionRuleDefinition()));
            menu.AddItem(
                new GUIContent("Ordered Actions"),
                _ruleDefinition.managedReferenceValue is OrderedActionsRuleDefinition,
                () => AssignRule(new OrderedActionsRuleDefinition()));
            menu.AddItem(
                new GUIContent("Guarded Action"),
                _ruleDefinition.managedReferenceValue is GuardedActionRuleDefinition,
                () => AssignRule(new GuardedActionRuleDefinition()));
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
                    EditorGUILayout.HelpBox(
                        "Select a completion rule for the step.",
                        MessageType.Warning);
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
            switch (_ruleDefinition.managedReferenceValue)
            {
                case SingleActionRuleDefinition:
                    return "Single Action";
                case OrderedActionsRuleDefinition:
                    return "Ordered Actions";
                case GuardedActionRuleDefinition:
                    return "Guarded Action";
                default:
                    return "Select Rule";
            }
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
