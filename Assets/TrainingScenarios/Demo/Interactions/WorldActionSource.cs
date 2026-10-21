using UnityEngine;
using TrainingScenarios.Runtime.Actions.Models;
using TrainingScenarios.Runtime.Actions.Sources;

namespace TrainingScenarios.Demo.Interactions
{
    public sealed class WorldActionSource : ActionSourceBehaviour
    {
        [SerializeField]
        private string _actionType = ScenarioActionTypes.ButtonClicked;

        public void Raise()
        {
            RaiseAction(_actionType);
        }
    }
}
