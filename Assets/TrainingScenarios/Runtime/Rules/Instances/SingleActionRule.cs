using TrainingScenarios.Runtime.Actions.Models;
using TrainingScenarios.Runtime.Rules.Contracts;
using TrainingScenarios.Runtime.Rules.Models;

namespace TrainingScenarios.Runtime.Rules.Instances
{
    public sealed class SingleActionRule : IStepRule
    {
        private readonly ActionRequirement _expectedAction;
        private readonly string _violationReason;

        public SingleActionRule(ActionRequirement expectedAction, string violationReason)
        {
            _expectedAction = expectedAction;
            _violationReason = violationReason;
        }

        public bool IsRelated(ScenarioAction action)
        {
            return _expectedAction.IsRelated(action);
        }

        public StepEvaluation Evaluate(ScenarioAction action, IScenarioContext context)
        {
            if (_expectedAction.Matches(action))
            {
                return StepEvaluation.Success();
            }

            return _expectedAction.IsRelated(action)
                ? StepEvaluation.Violation(_violationReason)
                : StepEvaluation.NotRelevant();
        }
    }
}
