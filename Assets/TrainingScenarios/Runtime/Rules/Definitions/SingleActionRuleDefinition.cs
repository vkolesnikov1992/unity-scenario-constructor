using System;
using UnityEngine;
using TrainingScenarios.Runtime.Actions.Models;
using TrainingScenarios.Runtime.Rules.Contracts;
using TrainingScenarios.Runtime.Rules.Instances;

namespace TrainingScenarios.Runtime.Rules.Definitions
{
    [Serializable]
    public sealed class SingleActionRuleDefinition : IStepRuleDefinition
    {
        [SerializeField]
        private ActionRequirement _expectedAction = new ActionRequirement();
        [SerializeField]
        private string _violationReason = "An unexpected action was received.";

        public SingleActionRuleDefinition()
        {
        }

        public SingleActionRuleDefinition(
            ActionRequirement expectedAction,
            string violationReason)
        {
            _expectedAction = expectedAction;
            _violationReason = violationReason;
        }

        public ActionRequirement ExpectedAction => _expectedAction;

        public string ViolationReason => _violationReason;

        public IStepRule CreateRuntime()
        {
            return new SingleActionRule(_expectedAction, _violationReason);
        }
    }
}
