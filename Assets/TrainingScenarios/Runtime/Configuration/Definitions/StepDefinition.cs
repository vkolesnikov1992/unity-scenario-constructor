using UnityEngine;
using TrainingScenarios.Runtime.Core.Steps;
using TrainingScenarios.Runtime.Rules.Contracts;

namespace TrainingScenarios.Runtime.Configuration.Definitions
{
    [CreateAssetMenu(
        fileName = "StepDefinition",
        menuName = "Training Scenarios/Step Definition")]
    public sealed class StepDefinition : ScriptableObject, IStepDefinition
    {
        [SerializeField]
        private string _stepId;
        [SerializeField]
        private string _displayName;
        [SerializeField]
        [TextArea(2, 5)]
        private string _instruction;
        [SerializeField]
        private string _uiHint;
        [SerializeReference]
        private IStepRuleDefinition _ruleDefinition;

        public string StepId => _stepId;

        public string DisplayName => _displayName;

        public string Instruction => _instruction;

        public string UiHint => _uiHint;

        public IStepRuleDefinition RuleDefinition => _ruleDefinition;
    }
}
