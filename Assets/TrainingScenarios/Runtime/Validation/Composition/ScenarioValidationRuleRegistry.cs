using System.Collections.Generic;
using TrainingScenarios.Runtime.Validation.Contracts;
using TrainingScenarios.Runtime.Validation.Rules.Activation;
using TrainingScenarios.Runtime.Validation.Rules.Bindings;
using TrainingScenarios.Runtime.Validation.Rules.Exam;
using TrainingScenarios.Runtime.Validation.Rules.Providers;
using TrainingScenarios.Runtime.Validation.Rules.Scenario;
using TrainingScenarios.Runtime.Validation.Rules.Scoring;

namespace TrainingScenarios.Runtime.Validation.Composition
{
    public static class ScenarioValidationRuleRegistry
    {
        public static IReadOnlyList<IScenarioValidationRule> CreateDefault()
        {
            return new IScenarioValidationRule[]
            {
                new ScenarioIdentityValidationRule(),
                new StepDefinitionsValidationRule(),
                new ActivationPlanValidationRule(),
                new ExamGroupsValidationRule(),
                new ActivationExamCompatibilityValidationRule(),
                new ScoringValidationRule(),
                new SceneBindingsValidationRule(),
                new ProviderValidationRule()
            };
        }
    }
}
