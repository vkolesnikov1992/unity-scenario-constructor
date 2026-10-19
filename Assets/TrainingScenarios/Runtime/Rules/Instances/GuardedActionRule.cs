using System;
using TrainingScenarios.Runtime.Actions.Models;
using TrainingScenarios.Runtime.Rules.Contracts;
using TrainingScenarios.Runtime.Rules.Models;

namespace TrainingScenarios.Runtime.Rules.Instances
{
    public sealed class GuardedActionRule : IStepRule
    {
        private readonly ActionRequirement _expectedAction;
        private readonly string _requiredStateId;
        private readonly string _requiredStateValue;
        private readonly string _guardViolationReason;
        private readonly string _unexpectedActionReason;

        public GuardedActionRule(
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

        public StepEvaluation Evaluate(ScenarioAction action, IScenarioContext context)
        {
            if (!_expectedAction.Matches(action))
            {
                return _expectedAction.IsRelated(action)
                    ? StepEvaluation.Violation(_unexpectedActionReason)
                    : StepEvaluation.NotRelevant();
            }

            var guardSatisfied =
                context.TryGetState(_requiredStateId, out var currentValue) &&
                string.Equals(currentValue, _requiredStateValue, StringComparison.Ordinal);

            return guardSatisfied
                ? StepEvaluation.Success()
                : StepEvaluation.Violation(_guardViolationReason);
        }
    }
}
