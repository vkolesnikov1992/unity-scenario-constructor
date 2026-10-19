using TrainingScenarios.Runtime.Actions.Models;

namespace TrainingScenarios.Runtime.Actions.Sources
{
    public sealed class UnityEventActionSource : ActionSourceBehaviour
    {
        public void Raise()
        {
            RaiseAction(ScenarioActionTypes.UnityEventInvoked);
        }
    }
}
