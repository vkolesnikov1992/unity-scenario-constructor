using System;
using UnityEngine;
using TrainingScenarios.Runtime.Actions.Models;
using TrainingScenarios.Runtime.Rules.Contracts;
using TrainingScenarios.Runtime.Rules.Instances;

namespace TrainingScenarios.Runtime.Rules.Definitions
{
    [Serializable]
    public sealed class GuardedActionRuleDefinition : IStepRuleDefinition
    {
        [SerializeField]
        private ActionRequirement _expectedAction = new ActionRequirement();
        [SerializeField]
        private string _requiredStateId;
        [SerializeField]
        private string _requiredStateValue;
        [SerializeField]
        private string _guardViolationReason = "Set the related object to the required state first.";
        [SerializeField]
        private string _unexpectedActionReason = "An unexpected action was received.";

        public GuardedActionRuleDefinition()
        {
        }

        public GuardedActionRuleDefinition(
            ActionRequirement expectedAction,
            string requiredStateId,
            string requiredStateValue,
            string guardViolationReason,
            string unexpectedActionReason)
        {
            _expectedAction = expectedAction;
            _requiredStateId = requiredStateId;
            _requiredStateValue = requiredStateValue;
            _guardViolationReason = guardViolationReason;
            _unexpectedActionReason = unexpectedActionReason;
        }

        public ActionRequirement ExpectedAction => _expectedAction;

        public string RequiredStateId => _requiredStateId;

        public string RequiredStateValue => _requiredStateValue;

        public string GuardViolationReason => _guardViolationReason;

        public string UnexpectedActionReason => _unexpectedActionReason;

        public IStepRule CreateRuntime()
        {
            return new GuardedActionRule(
                _expectedAction,
                _requiredStateId,
                _requiredStateValue,
                _guardViolationReason,
                _unexpectedActionReason);
        }
    }
}
