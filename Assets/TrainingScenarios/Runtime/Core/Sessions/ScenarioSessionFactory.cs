using System;
using System.Collections.Generic;
using TrainingScenarios.Runtime.Core.Steps;
using TrainingScenarios.Runtime.Execution.Activation;

namespace TrainingScenarios.Runtime.Core.Sessions
{
    public sealed class ScenarioSessionFactory
    {
        private readonly Func<DateTimeOffset> _utcNow;
        private readonly Func<string> _sessionIdFactory;

        public ScenarioSessionFactory(
            Func<DateTimeOffset> utcNow = null,
            Func<string> sessionIdFactory = null)
        {
            _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
            _sessionIdFactory = sessionIdFactory ?? (() => Guid.NewGuid().ToString("N"));
        }

        public ScenarioSession Create(
            string scenarioId,
            IEnumerable<IStepDefinition> stepDefinitions,
            ActivationPlan activationPlan)
        {
            var runtimes = new Dictionary<string, StepRuntime>(StringComparer.Ordinal);

            foreach (var definition in stepDefinitions)
            {
                var ruleRuntime = definition.RuleDefinition.CreateRuntime();
                runtimes.Add(definition.StepId, new StepRuntime(definition.StepId, ruleRuntime));
            }

            var sessionId = _sessionIdFactory();
            return new ScenarioSession(sessionId, scenarioId, runtimes, activationPlan, _utcNow);
        }
    }
}
