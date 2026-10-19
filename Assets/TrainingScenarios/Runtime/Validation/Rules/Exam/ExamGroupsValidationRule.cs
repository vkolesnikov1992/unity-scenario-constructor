using System;
using System.Collections.Generic;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Validation.Contracts;
using TrainingScenarios.Runtime.Validation.Models;

namespace TrainingScenarios.Runtime.Validation.Rules.Exam
{
    public sealed class ExamGroupsValidationRule : IScenarioValidationRule
    {
        public IEnumerable<ValidationIssue> Validate(ScenarioValidationContext context)
        {
            var scenario = context.Scenario;
            if (!scenario || !scenario.ExamProfile)
            {
                yield break;
            }

            var profile = scenario.ExamProfile;
            if (profile.Groups.Count == 0)
            {
                yield return new ValidationIssue(
                    "EXAM_GROUPS_EMPTY",
                    ValidationSeverity.Error,
                    "The exam profile does not contain any exam groups.",
                    profile);
            }

            if (profile.ViolationThreshold <= 0)
            {
                yield return new ValidationIssue(
                    "EXAM_THRESHOLD_INVALID",
                    ValidationSeverity.Error,
                    "The exam violation threshold must be greater than zero.",
                    profile);
            }

            var knownSteps = new HashSet<StepDefinition>();
            foreach (var step in scenario.Steps)
            {
                if (step)
                {
                    knownSteps.Add(step);
                }
            }

            var assignments = new Dictionary<StepDefinition, int>();
            var groupIds = new HashSet<string>(StringComparer.Ordinal);

            for (var groupIndex = 0; groupIndex < profile.Groups.Count; groupIndex++)
            {
                var group = profile.Groups[groupIndex];
                if (group == null || group.Steps.Count == 0)
                {
                    yield return new ValidationIssue(
                        "EXAM_GROUP_EMPTY",
                        ValidationSeverity.Error,
                        $"Exam group {groupIndex + 1} does not contain steps.",
                        profile);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(group.GroupId))
                {
                    yield return new ValidationIssue(
                        "EXAM_GROUP_ID_MISSING",
                        ValidationSeverity.Error,
                        $"Exam group {groupIndex + 1} does not have a stable ID.",
                        profile);
                }
                else if (!groupIds.Add(group.GroupId))
                {
                    yield return new ValidationIssue(
                        "EXAM_GROUP_ID_DUPLICATE",
                        ValidationSeverity.Error,
                        $"Exam group ID '{group.GroupId}' is used more than once.",
                        profile);
                }

                foreach (var step in group.Steps)
                {
                    if (!step)
                    {
                        yield return new ValidationIssue(
                            "EXAM_GROUP_STEP_REFERENCE_MISSING",
                            ValidationSeverity.Error,
                            $"Exam group {groupIndex + 1} contains an empty step reference.",
                            profile);
                        continue;
                    }

                    if (!knownSteps.Contains(step))
                    {
                        yield return new ValidationIssue(
                            "EXAM_GROUP_STEP_UNKNOWN",
                            ValidationSeverity.Error,
                            $"Exam group {groupIndex + 1} contains unknown step " +
                            $"'{step.DisplayName}'.",
                            step);
                    }

                    assignments.TryGetValue(step, out var assignmentCount);
                    assignments[step] = assignmentCount + 1;
                }
            }

            foreach (var step in knownSteps)
            {
                assignments.TryGetValue(step, out var assignmentCount);

                if (assignmentCount == 0)
                {
                    yield return new ValidationIssue(
                        "EXAM_STEP_NOT_ASSIGNED",
                        ValidationSeverity.Error,
                        $"Step '{step.DisplayName}' is not assigned to an exam group.",
                        step);
                }
                else if (assignmentCount > 1)
                {
                    yield return new ValidationIssue(
                        "EXAM_STEP_ASSIGNED_MULTIPLE_TIMES",
                        ValidationSeverity.Error,
                        $"Step '{step.DisplayName}' is assigned to more than one exam group.",
                        step);
                }
            }
        }
    }
}
