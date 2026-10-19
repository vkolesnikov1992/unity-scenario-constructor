using System;
using System.Collections.Generic;
using UnityEngine;
using TrainingScenarios.Runtime.Configuration.Profiles;
using TrainingScenarios.Runtime.Configuration.Providers;
using TrainingScenarios.Runtime.Execution.Modes.Models;

namespace TrainingScenarios.Runtime.Configuration.Definitions
{
    [CreateAssetMenu(
        fileName = "ScenarioDefinition",
        menuName = "Training Scenarios/Scenario Definition")]
    public sealed class ScenarioDefinition : ScriptableObject
    {
        [SerializeField]
        private string _scenarioId;
        [SerializeField]
        private string _displayName;
        [SerializeField]
        [TextArea(2, 5)]
        private string _description;
        [SerializeField]
        private List<StepDefinition> _steps = new List<StepDefinition>();
        [SerializeField]
        private TrainingModeProfile _trainingProfile;
        [SerializeField]
        private ExamModeProfile _examProfile;
        [SerializeField]
        private LoggerProvider _loggerProvider;

        public string ScenarioId => _scenarioId;

        public string DisplayName => _displayName;

        public string Description => _description;

        public IReadOnlyList<StepDefinition> Steps => _steps;

        public TrainingModeProfile TrainingProfile => _trainingProfile;

        public ExamModeProfile ExamProfile => _examProfile;

        public LoggerProvider LoggerProvider => _loggerProvider;

        public ScenarioModeProfile GetProfile(ScenarioMode mode)
        {
            switch (mode)
            {
                case ScenarioMode.Training:
                    return _trainingProfile;
                case ScenarioMode.Exam:
                    return _examProfile;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
    }
}
