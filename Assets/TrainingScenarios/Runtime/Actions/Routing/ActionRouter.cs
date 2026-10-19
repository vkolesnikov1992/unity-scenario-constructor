using System;
using System.Collections.Generic;
using System.Linq;
using TrainingScenarios.Runtime.Actions.Contracts;
using TrainingScenarios.Runtime.Actions.Models;

namespace TrainingScenarios.Runtime.Actions.Routing
{
    public sealed class ActionRouter : IDisposable
    {
        private readonly IReadOnlyList<IActionSource> _sources;
        private bool _isRunning;

        public ActionRouter(IEnumerable<IActionSource> sources)
        {
            _sources = sources.ToArray();
        }

        public event Action<ScenarioAction> ActionReceived;

        public void Start()
        {
            if (_isRunning)
            {
                return;
            }

            foreach (var source in _sources)
            {
                source.ActionRaised += HandleActionRaised;
            }

            _isRunning = true;
        }

        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }

            foreach (var source in _sources)
            {
                source.ActionRaised -= HandleActionRaised;
            }

            _isRunning = false;
        }

        public void Dispose()
        {
            Stop();
        }

        private void HandleActionRaised(ScenarioAction action)
        {
            ActionReceived?.Invoke(action);
        }
    }
}
