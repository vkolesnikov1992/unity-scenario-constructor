using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using TrainingScenarios.Runtime.Logging.Snapshots;

namespace TrainingScenarios.Runtime.Logging.Models
{
    [DataContract]
    public sealed class SessionJournal
    {
        private static readonly IReadOnlyList<ScenarioEvent> EmptyEvents =
            new ReadOnlyCollection<ScenarioEvent>(Array.Empty<ScenarioEvent>());

        [DataMember(Name = "schemaVersion", Order = 1)]
        private int _schemaVersion;

        [DataMember(Name = "sessionId", Order = 2)]
        private string _sessionId;

        [DataMember(Name = "scenarioId", Order = 3)]
        private string _scenarioId;

        [DataMember(Name = "snapshot", Order = 4, EmitDefaultValue = false)]
        private SessionSnapshot _snapshot;

        [DataMember(Name = "events", Order = 5)]
        private List<ScenarioEvent> _events;

        public SessionJournal(
            string sessionId,
            string scenarioId,
            SessionSnapshot snapshot)
        {
            _schemaVersion = JournalSchema.CurrentVersion;
            _sessionId = sessionId;
            _scenarioId = scenarioId;
            _snapshot = snapshot;
            _events = new List<ScenarioEvent>();
        }

        public int SchemaVersion => _schemaVersion;

        public string SessionId => _sessionId;

        public string ScenarioId => _scenarioId;

        public SessionSnapshot Snapshot => _snapshot;

        public IReadOnlyList<ScenarioEvent> Events => _events ?? EmptyEvents;

        public bool IsCompleted => Events.Any(entry =>
            entry != null &&
            string.Equals(
                entry.EventType,
                ScenarioEventType.SessionCompleted.ToString(),
                StringComparison.Ordinal));

        public void Append(ScenarioEvent entry)
        {
            _events.Add(entry);
        }
    }
}
