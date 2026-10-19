using System.Collections.Generic;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Configuration.Flow;
using TrainingScenarios.Runtime.Validation.Contracts;
using TrainingScenarios.Runtime.Validation.Models;

namespace TrainingScenarios.Runtime.Validation.Rules.Exam
{
    public sealed class ActivationExamCompatibilityValidationRule : IScenarioValidationRule
    {
        public IEnumerable<ValidationIssue> Validate(ScenarioValidationContext context)
        {
            var scenario = context.Scenario;
            if (!scenario ||
                !scenario.ExamProfile ||
                scenario.ExamProfile.ActivationPlan == null)
            {
                yield break;
            }

            var profile = scenario.ExamProfile;
            var groupIndexByStep = CreateGroupIndex(profile.Groups);
            var previousGroupIndex = -1;

            for (var stageIndex = 0;
                 stageIndex < profile.ActivationPlan.Stages.Count;
                 stageIndex++)
            {
                var stage = profile.ActivationPlan.Stages[stageIndex];
                if (stage == null)
                {
                    continue;
                }

                var stageGroupIndices = new HashSet<int>();
                foreach (var step in stage.Steps)
                {
                    if (step && groupIndexByStep.TryGetValue(step, out var groupIndex))
                    {
                        stageGroupIndices.Add(groupIndex);
                    }
                }

                if (stageGroupIndices.Count > 1)
                {
                    yield return new ValidationIssue(
                        "ACTIVATION_STAGE_CROSSES_EXAM_GROUPS",
                        ValidationSeverity.Error,
                        $"Exam activation stage {stageIndex + 1} contains steps from " +
                        "different exam groups.",
                        profile);
                    continue;
                }

                foreach (var groupIndex in stageGroupIndices)
                {
                    if (groupIndex < previousGroupIndex)
                    {
                        yield return new ValidationIssue(
                            "ACTIVATION_EXAM_GROUP_ORDER_INVALID",
                            ValidationSeverity.Error,
                            $"Exam activation stage {stageIndex + 1} returns to an earlier " +
                            "exam group.",
                            profile);
                    }

                    previousGroupIndex = groupIndex;
                }
            }
        }

        private static Dictionary<StepDefinition, int> CreateGroupIndex(
            IReadOnlyList<ExamGroupConfiguration> groups)
        {
            var result = new Dictionary<StepDefinition, int>();

            for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                var group = groups[groupIndex];
                if (group == null)
                {
                    continue;
                }

                foreach (var step in group.Steps)
                {
                    if (step && !result.ContainsKey(step))
                    {
                        result.Add(step, groupIndex);
                    }
                }
            }

            return result;
        }
    }
}
