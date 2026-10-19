using System;
using TrainingScenarios.Runtime.Actions.Models;

namespace TrainingScenarios.Runtime.Actions.Contracts
{
    public interface IActionSource
    {
        event Action<ScenarioAction> ActionRaised;
    }
}
