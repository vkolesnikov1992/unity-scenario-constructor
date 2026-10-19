using System.Linq;
using TrainingScenarios.Runtime.Actions.Bindings;
using TrainingScenarios.Runtime.Actions.Routing;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Core.Sessions;
using TrainingScenarios.Runtime.Execution.Modes.Models;
using TrainingScenarios.Runtime.Execution.Runner;
using TrainingScenarios.Runtime.Logging.Models;
using TrainingScenarios.Runtime.Logging.Recording;
using TrainingScenarios.Runtime.Logging.Sinks;
using TrainingScenarios.Runtime.Validation;
using TrainingScenarios.Runtime.Validation.Composition;
using TrainingScenarios.Runtime.Validation.Models;

namespace TrainingScenarios.Runtime.Composition
{
    public sealed class ScenarioRuntimeFactory
    {
        private readonly ScenarioSessionFactory _sessionFactory;
        private readonly ScenarioValidator _validator;

        public ScenarioRuntimeFactory()
            : this(
                new ScenarioSessionFactory(),
                new ScenarioValidator(ScenarioValidationRuleRegistry.CreateDefault()))
        {
        }

        public ScenarioRuntimeFactory(
            ScenarioSessionFactory sessionFactory,
            ScenarioValidator validator)
        {
            _sessionFactory = sessionFactory;
            _validator = validator;
        }

        public ScenarioRuntimeCreationResult Create(
            ScenarioDefinition scenario,
            ScenarioMode mode,
            SceneBindingRegistry sceneBindings)
        {
            var validationContext = new ScenarioValidationContext(scenario, sceneBindings);
            var validationIssues = _validator.Validate(validationContext);

            if (validationIssues.Any(issue => issue.Severity == ValidationSeverity.Error))
            {
                return new ScenarioRuntimeCreationResult(null, validationIssues);
            }

            sceneBindings.Rebuild();

            var profile = scenario.GetProfile(mode);
            var session = _sessionFactory.Create(
                scenario.ScenarioId,
                scenario.Steps,
                profile.ActivationPlan.CreateRuntime());
            var policy = profile.CreatePolicy();
            var actionRouter = new ActionRouter(sceneBindings.ActionSources);
            var runner = new ScenarioRunner(
                session,
                actionRouter,
                sceneBindings,
                policy,
                profile.InactiveActionBehavior);
            var snapshot = profile.CreateSnapshot(scenario.Steps);
            var journal = new SessionJournal(
                session.SessionId,
                session.ScenarioId,
                snapshot);
            var recorder = new ScenarioJournalRecorder(
                runner,
                journal,
                new MemoryEventSink(journal));
            var journalStore = scenario.LoggerProvider.CreateStore();
            var analyzer = profile.AnalyzerProvider.CreateAnalyzer();
            var runtime = new ScenarioRuntime(
                runner,
                recorder,
                journal,
                journalStore,
                analyzer);

            return new ScenarioRuntimeCreationResult(runtime, validationIssues);
        }
    }
}
