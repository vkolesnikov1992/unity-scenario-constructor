using TrainingScenarios.Runtime.Actions.Models;
using TrainingScenarios.Runtime.Rules.Contracts;
using TrainingScenarios.Runtime.Rules.Models;

namespace TrainingScenarios.Runtime.Rules.Instances
{
    public sealed class OrderedActionsRule : IStepRule
    {
        private readonly ActionRequirement _firstAction;
        private readonly ActionRequirement _secondAction;
        private readonly string _orderViolationReason;
        private readonly string _unexpectedActionReason;

        private bool _isFirstActionCompleted;

        public OrderedActionsRule(
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

        public StepEvaluation Evaluate(ScenarioAction action, IScenarioContext context)
        {
            if (_firstAction.Matches(action))
            {
                _isFirstActionCompleted = true;
                return StepEvaluation.Progress();
            }

            if (_secondAction.Matches(action))
            {
                return _isFirstActionCompleted
                    ? StepEvaluation.Success()
                    : StepEvaluation.Violation(_orderViolationReason);
            }

            return IsRelated(action)
                ? StepEvaluation.Violation(_unexpectedActionReason)
                : StepEvaluation.NotRelevant();
        }

        private bool IsRelated(ScenarioAction action)
        {
            return _firstAction.IsRelated(action) || _secondAction.IsRelated(action);
        }
    }
}
