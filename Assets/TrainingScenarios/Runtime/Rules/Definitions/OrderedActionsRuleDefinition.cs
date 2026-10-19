using System;
using UnityEngine;
using TrainingScenarios.Runtime.Actions.Models;
using TrainingScenarios.Runtime.Rules.Contracts;
using TrainingScenarios.Runtime.Rules.Instances;

namespace TrainingScenarios.Runtime.Rules.Definitions
{
    [Serializable]
    public sealed class OrderedActionsRuleDefinition : IStepRuleDefinition
    {
        [SerializeField]
        private ActionRequirement _firstAction = new ActionRequirement();
        [SerializeField]
        private ActionRequirement _secondAction = new ActionRequirement();
        [SerializeField]
        private string _orderViolationReason = "Complete the first action in the sequence first.";
        [SerializeField]
        private string _unexpectedActionReason = "The action does not match the expected sequence.";

        public OrderedActionsRuleDefinition()
        {
        }

        public OrderedActionsRuleDefinition(
            ActionRequirement firstAction,
            ActionRequirement secondAction,
            string orderViolationReason,
            string unexpectedActionReason)
        {
            _firstAction = firstAction;
            _secondAction = secondAction;
            _orderViolationReason = orderViolationReason;
            _unexpectedActionReason = unexpectedActionReason;
        }

        public ActionRequirement FirstAction => _firstAction;

        public ActionRequirement SecondAction => _secondAction;

        public string OrderViolationReason => _orderViolationReason;

        public string UnexpectedActionReason => _unexpectedActionReason;

        public IStepRule CreateRuntime()
        {
            return new OrderedActionsRule(
                _firstAction,
                _secondAction,
                _orderViolationReason,
                _unexpectedActionReason);
        }
    }
}
