using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TrainingScenarios.Runtime.Actions.Models
{
    public sealed class ScenarioAction
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyData =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

        public ScenarioAction(
            string sourceId,
            string actionType,
            string targetId,
            DateTimeOffset timestamp,
            IReadOnlyDictionary<string, string> additionalData = null)
        {
            SourceId = sourceId;
            ActionType = actionType;
            TargetId = targetId;
            Timestamp = timestamp;
            AdditionalData = CopyAdditionalData(additionalData);
        }

        public string SourceId { get; }

        public string ActionType { get; }

        public string TargetId { get; }

        public DateTimeOffset Timestamp { get; }

        public IReadOnlyDictionary<string, string> AdditionalData { get; }

        private static IReadOnlyDictionary<string, string> CopyAdditionalData(
            IReadOnlyDictionary<string, string> additionalData)
        {
            if (additionalData == null || additionalData.Count == 0)
            {
                return EmptyData;
            }

            return new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(additionalData));
        }
    }
}
