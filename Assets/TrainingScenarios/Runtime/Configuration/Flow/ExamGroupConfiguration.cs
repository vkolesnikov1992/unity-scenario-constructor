using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Execution.Modes.Exam;
using TrainingScenarios.Runtime.Logging.Snapshots;

namespace TrainingScenarios.Runtime.Configuration.Flow
{
    [Serializable]
    public sealed class ExamGroupConfiguration
    {
        [SerializeField]
        private string _groupId;
        [SerializeField]
        private string _displayName;
        [SerializeField]
        private List<StepDefinition> _steps = new List<StepDefinition>();

        public string GroupId => _groupId;

        public string DisplayName => _displayName;

        public IReadOnlyList<StepDefinition> Steps => _steps;

        public ExamGroup CreateRuntime()
        {
            return new ExamGroup(_groupId, _steps.Select(step => step.StepId));
        }

        public ExamGroupSnapshot CreateSnapshot()
        {
            return new ExamGroupSnapshot(_groupId, _steps.Select(step => step.StepId));
        }
    }
}
