using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace TrainingScenarios.Runtime.Logging.Models
{
    [DataContract]
    public sealed class ScenarioEvent
    {
        private static readonly IReadOnlyList<JournalDataEntry> EmptyData =
            new ReadOnlyCollection<JournalDataEntry>(Array.Empty<JournalDataEntry>());

        [DataMember(Name = "schemaVersion", Order = 1)]
        private int _schemaVersion;

        [DataMember(Name = "sessionId", Order = 2)]
        private string _sessionId;

        [DataMember(Name = "scenarioId", Order = 3)]
        private string _scenarioId;

        [DataMember(Name = "timestampUtc", Order = 4)]
        private string _timestampUtc;

        [DataMember(Name = "eventType", Order = 5)]
        private string _eventType;

        [DataMember(Name = "stepId", Order = 6)]
        private string _stepId;

        [DataMember(Name = "result", Order = 7)]
        private string _result;

        [DataMember(Name = "attemptNumber", Order = 8)]
        private int _attemptNumber;

        [DataMember(Name = "groupId", Order = 9)]
        private string _groupId;

        [DataMember(Name = "additionalData", Order = 10)]
        private List<JournalDataEntry> _additionalData;

        public ScenarioEvent(
            string sessionId,
            string scenarioId,
            DateTimeOffset timestamp,
            ScenarioEventType eventType,
            string stepId,
            string result,
            int attemptNumber,
            string groupId,
            IEnumerable<JournalDataEntry> additionalData)
        {
            _schemaVersion = JournalSchema.CurrentVersion;
            _sessionId = sessionId;
            _scenarioId = scenarioId;
            _timestampUtc = timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
            _eventType = eventType.ToString();
            _stepId = stepId;
            _result = result;
            _attemptNumber = attemptNumber;
            _groupId = groupId;
            _additionalData = additionalData.ToList();
        }

        public int SchemaVersion => _schemaVersion;

        public string SessionId => _sessionId;

        public string ScenarioId => _scenarioId;

        public string TimestampUtc => _timestampUtc;

        public string EventType => _eventType;

        public string StepId => _stepId;

        public string Result => _result;

        public int AttemptNumber => _attemptNumber;

        public string GroupId => _groupId;

        public IReadOnlyList<JournalDataEntry> AdditionalData => _additionalData ?? EmptyData;
    }
}
