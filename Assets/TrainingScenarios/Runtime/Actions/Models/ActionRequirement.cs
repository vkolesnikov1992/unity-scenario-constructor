using System;
using UnityEngine;

namespace TrainingScenarios.Runtime.Actions.Models
{
    [Serializable]
    public sealed class ActionRequirement
    {
        [SerializeField]
        private string _sourceId;
        [SerializeField]
        private string _actionType;
        [SerializeField]
        private string _targetId;

        public ActionRequirement()
        {
        }

        public ActionRequirement(string sourceId, string actionType, string targetId)
        {
            _sourceId = sourceId;
            _actionType = actionType;
            _targetId = targetId;
        }

        public string SourceId => _sourceId;

        public string ActionType => _actionType;

        public string TargetId => _targetId;

        public bool Matches(ScenarioAction action)
        {
            return IsRelated(action) &&
                   string.Equals(_actionType, action.ActionType, StringComparison.Ordinal);
        }

        public bool IsRelated(ScenarioAction action)
        {
            return string.Equals(_sourceId, action.SourceId, StringComparison.Ordinal) &&
                   string.Equals(_targetId, action.TargetId, StringComparison.Ordinal);
        }
    }
}
