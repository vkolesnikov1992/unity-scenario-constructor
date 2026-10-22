using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using TrainingScenarios.Runtime.Actions.Models;
using TrainingScenarios.Runtime.Actions.Routing;
using TrainingScenarios.Runtime.Analysis.Analyzers;
using TrainingScenarios.Runtime.Core.Sessions;
using TrainingScenarios.Runtime.Core.Steps;
using TrainingScenarios.Runtime.Execution.Activation;
using TrainingScenarios.Runtime.Execution.Modes.Exam;
using TrainingScenarios.Runtime.Execution.Modes.Models;
using TrainingScenarios.Runtime.Execution.Modes.Training;
using TrainingScenarios.Runtime.Execution.Runner;
using TrainingScenarios.Runtime.Logging.Models;
using TrainingScenarios.Runtime.Logging.Recording;
using TrainingScenarios.Runtime.Logging.Sinks;
using TrainingScenarios.Runtime.Logging.Snapshots;
using TrainingScenarios.Runtime.Rules.Contracts;
using TrainingScenarios.Runtime.Rules.Definitions;
using TrainingScenarios.Runtime.Rules.Models;
using TrainingScenarios.Runtime.Validation.Models;
using TrainingScenarios.Runtime.Validation.Rules.Scenario;

namespace TrainingScenarios.Tests.EditMode
{
    public sealed class ScenarioCoreTests
    {
        private static readonly DateTimeOffset Timestamp =
            new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        [Test]
        public void TrainingAnalyzer_FirstSecondAndThirdAttempts_Returns15()
        {
            var journal = CreateJournal(
                ScenarioMode.Training,
                new[] { "step-1", "step-2", "step-3" },
                new TrainingScoringSnapshot(),
                null);

            AppendTerminalEvent(journal, ScenarioEventType.StepCompleted, "step-1", 1);
            AppendTerminalEvent(journal, ScenarioEventType.StepCompleted, "step-2", 2);
            AppendTerminalEvent(journal, ScenarioEventType.StepCompleted, "step-3", 3);
            AppendSessionCompleted(journal);

            var result = new TrainingResultAnalyzer().Analyze(journal);

            Assert.That(result.IsCompatible, Is.True);
            Assert.That(result.Score, Is.EqualTo(15));
            Assert.That(result.MaximumScore, Is.EqualTo(30));
        }

        [Test]
        public void ExamAnalyzer_TwoFailedAndTwoSkipped_Returns70()
        {
            var stepIds = new[] { "failed-1", "failed-2", "skipped-1", "skipped-2" };
            var journal = CreateJournal(
                ScenarioMode.Exam,
                stepIds,
                null,
                new ExamScoringSnapshot());

            AppendTerminalEvent(journal, ScenarioEventType.StepFailed, "failed-1", 1);
            AppendTerminalEvent(journal, ScenarioEventType.StepFailed, "failed-2", 1);
            AppendTerminalEvent(journal, ScenarioEventType.StepSkipped, "skipped-1", 0);
            AppendTerminalEvent(journal, ScenarioEventType.StepSkipped, "skipped-2", 0);
            AppendSessionCompleted(journal);

            var result = new ExamResultAnalyzer().Analyze(journal);

            Assert.That(result.IsCompatible, Is.True);
            Assert.That(result.Score, Is.EqualTo(70));
            Assert.That(result.MaximumScore, Is.EqualTo(100));
        }

        [Test]
        public void ExamPolicy_InactiveFutureStep_CountsViolationForCurrentGroup()
        {
            var stepIds = new[] { "current", "future", "future-peer" };
            var session = CreateSession(
                stepIds,
                new ActivationPlan(new[]
                {
                    new ActivationStage(new[] { "current" }),
                    new ActivationStage(new[] { "future", "future-peer" })
                }));
            var policy = new ExamModePolicy(new ExamGroupPlan(
                new[]
                {
                    new ExamGroup("group-1", new[] { "current" }),
                    new ExamGroup("group-2", new[] { "future", "future-peer" })
                },
                2));

            policy.Initialize(session);
            session.Start();
            policy.ApplyInactiveEvaluation(
                session,
                session.GetStep("future"),
                StepEvaluation.Violation("out of order"));

            Assert.That(session.GetStep("future").Status, Is.EqualTo(StepStatus.Failed));
            Assert.That(policy.GetGroupProgress(0).ViolationCount, Is.EqualTo(1));
            Assert.That(policy.GetGroupProgress(1).ViolationCount, Is.Zero);
            Assert.That(policy.CurrentGroupIndex, Is.Zero);
        }

        [Test]
        public void ActivationPlan_ParallelThenSequential_ActivatesExpectedStages()
        {
            var session = CreateSession(
                new[] { "parallel-1", "parallel-2", "last" },
                new ActivationPlan(new[]
                {
                    new ActivationStage(new[] { "parallel-1", "parallel-2" }),
                    new ActivationStage(new[] { "last" })
                }));

            session.Start();
            Assert.That(session.GetStep("parallel-1").Status, Is.EqualTo(StepStatus.Active));
            Assert.That(session.GetStep("parallel-2").Status, Is.EqualTo(StepStatus.Active));
            Assert.That(session.GetStep("last").Status, Is.EqualTo(StepStatus.Inactive));

            session.CompleteStep("parallel-1");
            Assert.That(session.GetStep("last").Status, Is.EqualTo(StepStatus.Inactive));

            session.CompleteStep("parallel-2");
            Assert.That(session.GetStep("last").Status, Is.EqualTo(StepStatus.Active));
        }

        [Test]
        public void JournalRecorder_OrderedActionProgress_PreservesStepAssociation()
        {
            var definition = new FakeStepDefinition(
                "ordered",
                new OrderedActionsRuleDefinition(
                    new ActionRequirement("source", "first", "target"),
                    new ActionRequirement("source", "second", "target"),
                    "first action required",
                    "unexpected action"));
            var session = CreateSession(
                new[] { definition },
                new ActivationPlan(new[]
                {
                    new ActivationStage(new[] { definition.StepId })
                }));
            var runner = new ScenarioRunner(
                session,
                new ActionRouter(Array.Empty<TrainingScenarios.Runtime.Actions.Contracts.IActionSource>()),
                new EmptyScenarioContext(),
                new TrainingModePolicy(),
                InactiveActionBehavior.RecordViolation);
            var journal = CreateJournal(
                ScenarioMode.Training,
                new[] { definition.StepId },
                new TrainingScoringSnapshot(),
                null);
            var recorder = new ScenarioJournalRecorder(runner, journal, new MemoryEventSink(journal));

            recorder.StartRecording();
            runner.Start();
            runner.ProcessAction(new ScenarioAction("source", "first", "target", Timestamp));

            var progress = journal.Events.Single(entry =>
                entry.EventType == ScenarioEventType.StepEvaluated.ToString() &&
                entry.Result == StepEvaluationOutcome.Progress.ToString());
            Assert.That(progress.StepId, Is.EqualTo(definition.StepId));
            Assert.That(progress.AttemptNumber, Is.EqualTo(1));

            runner.Dispose();
            recorder.Dispose();
        }

        [Test]
        public void ActionRoutesValidator_SharedSourceAndTarget_ReturnsError()
        {
            var scenario = ScriptableObject.CreateInstance<
                TrainingScenarios.Runtime.Configuration.Definitions.ScenarioDefinition>();
            var first = CreateStepAsset("first", "shared-source", "shared-target");
            var second = CreateStepAsset("second", "shared-source", "shared-target");

            try
            {
                var serializedScenario = new SerializedObject(scenario);
                var steps = serializedScenario.FindProperty("_steps");
                steps.arraySize = 2;
                steps.GetArrayElementAtIndex(0).objectReferenceValue = first;
                steps.GetArrayElementAtIndex(1).objectReferenceValue = second;
                serializedScenario.ApplyModifiedPropertiesWithoutUndo();

                var issues = new ActionRoutesValidationRule()
                    .Validate(new ScenarioValidationContext(scenario))
                    .ToArray();

                Assert.That(issues, Has.Length.EqualTo(1));
                Assert.That(issues[0].Code, Is.EqualTo("ACTION_ROUTE_AMBIGUOUS"));
                Assert.That(issues[0].Severity, Is.EqualTo(ValidationSeverity.Error));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
                UnityEngine.Object.DestroyImmediate(scenario);
            }
        }

        private static ScenarioSession CreateSession(
            IEnumerable<string> stepIds,
            ActivationPlan activationPlan)
        {
            return CreateSession(
                stepIds.Select(stepId => new FakeStepDefinition(
                    stepId,
                    new SingleActionRuleDefinition(
                        new ActionRequirement(stepId, "complete", stepId),
                        "unexpected"))),
                activationPlan);
        }

        private static ScenarioSession CreateSession(
            IEnumerable<IStepDefinition> definitions,
            ActivationPlan activationPlan)
        {
            return new ScenarioSessionFactory(() => Timestamp, () => "session")
                .Create("scenario", definitions, activationPlan);
        }

        private static SessionJournal CreateJournal(
            ScenarioMode mode,
            IEnumerable<string> stepIds,
            TrainingScoringSnapshot trainingScoring,
            ExamScoringSnapshot examScoring)
        {
            var ids = stepIds.ToArray();
            var groups = mode == ScenarioMode.Exam
                ? new[] { new ExamGroupSnapshot("group", ids) }
                : Array.Empty<ExamGroupSnapshot>();
            var snapshot = new SessionSnapshot(
                mode,
                InactiveActionBehavior.RecordViolation,
                ids.Select(stepId => new StepSnapshot(stepId)),
                groups,
                trainingScoring,
                examScoring);
            return new SessionJournal("session", "scenario", snapshot);
        }

        private static TrainingScenarios.Runtime.Configuration.Definitions.StepDefinition
            CreateStepAsset(string stepId, string sourceId, string targetId)
        {
            var step = ScriptableObject.CreateInstance<
                TrainingScenarios.Runtime.Configuration.Definitions.StepDefinition>();
            var serializedStep = new SerializedObject(step);
            serializedStep.FindProperty("_stepId").stringValue = stepId;
            serializedStep.FindProperty("_displayName").stringValue = stepId;
            serializedStep.FindProperty("_ruleDefinition").managedReferenceValue =
                new SingleActionRuleDefinition(
                    new ActionRequirement(sourceId, "complete", targetId),
                    "unexpected");
            serializedStep.ApplyModifiedPropertiesWithoutUndo();
            return step;
        }

        private static void AppendTerminalEvent(
            SessionJournal journal,
            ScenarioEventType eventType,
            string stepId,
            int attemptNumber)
        {
            journal.Append(new ScenarioEvent(
                journal.SessionId,
                journal.ScenarioId,
                Timestamp,
                eventType,
                stepId,
                eventType.ToString(),
                attemptNumber,
                "group",
                Array.Empty<JournalDataEntry>()));
        }

        private static void AppendSessionCompleted(SessionJournal journal)
        {
            journal.Append(new ScenarioEvent(
                journal.SessionId,
                journal.ScenarioId,
                Timestamp,
                ScenarioEventType.SessionCompleted,
                string.Empty,
                ScenarioSessionStatus.Completed.ToString(),
                0,
                string.Empty,
                Array.Empty<JournalDataEntry>()));
        }

        private sealed class FakeStepDefinition : IStepDefinition
        {
            public FakeStepDefinition(string stepId, IStepRuleDefinition ruleDefinition)
            {
                StepId = stepId;
                RuleDefinition = ruleDefinition;
            }

            public string StepId { get; }

            public IStepRuleDefinition RuleDefinition { get; }
        }

        private sealed class EmptyScenarioContext : IScenarioContext
        {
            public bool TryGetState(string stateId, out string value)
            {
                value = null;
                return false;
            }
        }
    }
}
