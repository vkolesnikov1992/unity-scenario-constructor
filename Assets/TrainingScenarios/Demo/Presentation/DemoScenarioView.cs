using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TrainingScenarios.Runtime.Composition;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Core.Steps;
using TrainingScenarios.Runtime.Execution.Modes.Models;
using TrainingScenarios.Runtime.Validation.Models;

namespace TrainingScenarios.Demo.Presentation
{
    public sealed class DemoScenarioView : MonoBehaviour
    {
        [SerializeField]
        private Text _modeText;
        [SerializeField]
        private Text _activeStepsText;
        [SerializeField]
        private Text _stepStatusesText;
        [SerializeField]
        private Text _messageText;
        [SerializeField]
        private Text _groupText;
        [SerializeField]
        private Text _resultText;
        [SerializeField]
        private Text _diagnosticsText;

        public void ShowReady()
        {
            _modeText.text = "Mode: not started";
            _activeStepsText.text = "Choose Training or Exam to begin.";
            _stepStatusesText.text = "0/12";
            _messageText.text = "Choose a mode to begin the scenario.";
            _groupText.text = "Group: —";
            _resultText.text = "Result: —";
            _diagnosticsText.text = "Configuration has not been validated yet.";
        }

        public void Render(
            ScenarioRuntime runtime,
            ScenarioDefinition scenario,
            string message)
        {
            _modeText.text = runtime.Runner.Mode == ScenarioMode.Training
                ? "Mode: Training"
                : "Mode: Exam";
            _messageText.text = string.IsNullOrWhiteSpace(message)
                ? "Waiting for an action."
                : message;

            RenderActiveSteps(runtime, scenario);
            RenderStepStatuses(runtime, scenario);
            RenderExamGroup(runtime, scenario);
            RenderResult(runtime);
        }

        public void ShowDiagnostics(
            System.Collections.Generic.IEnumerable<ValidationIssue> issues)
        {
            var issueList = issues.ToArray();
            _diagnosticsText.text = issueList.Length == 0
                ? "Configuration is valid."
                : "Diagnostics:\n" + string.Join(
                    "\n",
                    issueList.Select(issue =>
                        $"[{issue.Severity}] {issue.Code}: {issue.Message}"));
        }

        private void RenderActiveSteps(
            ScenarioRuntime runtime,
            ScenarioDefinition scenario)
        {
            var activeSteps = scenario.Steps
                .Where(step => runtime.Runner.Session.GetStep(step.StepId).Status == StepStatus.Active)
                .Select(step => $"<b>▶ {step.DisplayName}</b>\n   {step.Instruction}")
                .ToArray();

            _activeStepsText.text = activeSteps.Length == 0
                ? "No active objectives."
                : string.Join("\n\n", activeSteps);
        }

        private void RenderStepStatuses(
            ScenarioRuntime runtime,
            ScenarioDefinition scenario)
        {
            var steps = scenario.Steps
                .Select((definition, index) => new
                {
                    Number = index + 1,
                    Runtime = runtime.Runner.Session.GetStep(definition.StepId)
                })
                .ToArray();

            var terminalCount = steps.Count(item => item.Runtime.IsTerminal);
            var attemptCount = steps.Sum(item => item.Runtime.AttemptCount);
            var violationCount = steps.Sum(item => item.Runtime.ViolationCount);
            _stepStatusesText.text =
                $"{terminalCount}/{steps.Length}\n" +
                $"<size=13>Attempts: {attemptCount}   Violations: {violationCount}</size>";
        }

        private void RenderExamGroup(
            ScenarioRuntime runtime,
            ScenarioDefinition scenario)
        {
            if (runtime.Runner.Mode != ScenarioMode.Exam)
            {
                _groupText.text = "Group: all steps";
                return;
            }

            var activeGroup = scenario.ExamProfile.Groups.FirstOrDefault(group =>
                group.Steps.Any(step =>
                    runtime.Runner.Session.GetStep(step.StepId).Status == StepStatus.Active));

            _groupText.text = activeGroup == null
                ? "Group: completed"
                : $"Group: {activeGroup.DisplayName}";
        }

        private void RenderResult(ScenarioRuntime runtime)
        {
            var result = runtime.LastAnalysisResult;
            if (result == null)
            {
                _resultText.text = "Result: in progress.";
                return;
            }

            var steps = runtime.Runner.Session.Steps.Values;
            var completed = steps.Count(step => step.Status == StepStatus.Completed);
            var failed = steps.Count(step => step.Status == StepStatus.Failed);
            var skipped = steps.Count(step => step.Status == StepStatus.Skipped);

            _resultText.text =
                $"Result: {result.Score}/{result.MaximumScore}\n" +
                $"Completed {completed} | Failed {failed} | Skipped {skipped}\n" +
                result.Summary;
        }

        private static string GetStatusToken(StepStatus status)
        {
            switch (status)
            {
                case StepStatus.Active:
                    return "ACT";
                case StepStatus.Completed:
                    return "DONE";
                case StepStatus.Failed:
                    return "FAIL";
                case StepStatus.Skipped:
                    return "SKIP";
                default:
                    return "WAIT";
            }
        }
    }
}
