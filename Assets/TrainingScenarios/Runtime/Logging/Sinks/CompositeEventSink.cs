using System.Collections.Generic;
using System.Linq;
using TrainingScenarios.Runtime.Logging.Contracts;
using TrainingScenarios.Runtime.Logging.Models;

namespace TrainingScenarios.Runtime.Logging.Sinks
{
    public sealed class CompositeEventSink : IEventSink
    {
        private readonly IReadOnlyList<IEventSink> _sinks;

        public CompositeEventSink(IEnumerable<IEventSink> sinks)
        {
            _sinks = sinks.ToArray();
        }

        public void Append(ScenarioEvent entry)
        {
            foreach (var sink in _sinks)
            {
                sink.Append(entry);
            }
        }
    }
}
