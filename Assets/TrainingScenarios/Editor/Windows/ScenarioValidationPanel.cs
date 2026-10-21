using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TrainingScenarios.Editor.Validation;
using TrainingScenarios.Runtime.Actions.Bindings;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Validation;
using TrainingScenarios.Runtime.Validation.Composition;
using TrainingScenarios.Runtime.Validation.Models;

namespace TrainingScenarios.Editor.Windows
{
    public sealed class ScenarioValidationPanel
    {
        private IReadOnlyList<ValidationIssue> _issues = Array.Empty<ValidationIssue>();

        public bool HasResult { get; private set; }

        public bool HasErrors => !HasResult || _issues.Any(issue =>
            issue.Severity == ValidationSeverity.Error);

        public void Validate(ScenarioDefinition scenario)
        {
            Validate(ScenarioValidationContextFactory.Create(scenario));
        }

        public void Validate(
            ScenarioDefinition scenario,
            SceneBindingRegistry sceneBindings)
        {
            Validate(new ScenarioValidationContext(scenario, sceneBindings));
        }

        private void Validate(ScenarioValidationContext context)
        {
            var validator = new ScenarioValidator(
                ScenarioValidationRuleRegistry.CreateDefault());
            _issues = validator.Validate(context);
            HasResult = true;
        }

        public void Invalidate()
        {
            HasResult = false;
            _issues = Array.Empty<ValidationIssue>();
        }

        public void Draw()
        {
            EditorGUILayout.LabelField("Validation Results", EditorStyles.boldLabel);

            if (!HasResult)
            {
                EditorGUILayout.HelpBox(
                    "Validation has not been run, or the configuration has changed.",
                    MessageType.Info);
                return;
            }

            var errorCount = _issues.Count(issue =>
                issue.Severity == ValidationSeverity.Error);
            var warningCount = _issues.Count(issue =>
                issue.Severity == ValidationSeverity.Warning);

            if (errorCount > 0)
            {
                EditorGUILayout.HelpBox(
                    $"Errors found: {errorCount}. Play Mode launch is blocked.",
                    MessageType.Error);
            }
            else if (warningCount > 0)
            {
                EditorGUILayout.HelpBox(
                    $"Warnings found: {warningCount}. Play Mode launch is allowed.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Valid", MessageType.Info);
            }

            DrawIssueGroup("Errors", ValidationSeverity.Error, MessageType.Error);
            DrawIssueGroup("Warnings", ValidationSeverity.Warning, MessageType.Warning);
            DrawIssueGroup("Information", ValidationSeverity.Info, MessageType.Info);
        }

        private void DrawIssueGroup(
            string title,
            ValidationSeverity severity,
            MessageType messageType)
        {
            var issues = _issues
                .Where(issue => issue.Severity == severity)
                .ToArray();
            if (issues.Length == 0)
            {
                return;
            }

            EditorGUILayout.LabelField($"{title}: {issues.Length}", EditorStyles.boldLabel);

            foreach (var issue in issues)
            {
                EditorGUILayout.BeginHorizontal();
                var message = $"[{issue.Code}] {issue.Message}";
                if (issue.Context)
                {
                    message += $"\nObject: {issue.Context.name}";
                }

                EditorGUILayout.HelpBox(message, messageType);

                using (new EditorGUI.DisabledScope(!issue.Context))
                {
                    if (GUILayout.Button("Show", GUILayout.Width(80f)))
                    {
                        Selection.activeObject = issue.Context;
                        EditorGUIUtility.PingObject(issue.Context);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
