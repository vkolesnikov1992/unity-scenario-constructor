using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TrainingScenarios.Runtime.Analysis.Models;
using TrainingScenarios.Runtime.Analysis.Services;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Configuration.Providers;
using TrainingScenarios.Runtime.Logging.Contracts;
using TrainingScenarios.Runtime.Logging.Models;

namespace TrainingScenarios.Demo.Presentation
{
    public sealed class DemoJournalController : MonoBehaviour
    {
        [SerializeField]
        private ScenarioDefinition _scenario;
        [SerializeField]
        private List<AnalyzerProvider> _analyzerProviders = new List<AnalyzerProvider>();
        [SerializeField]
        private Dropdown _journalDropdown;
        [SerializeField]
        private Dropdown _analyzerDropdown;
        [SerializeField]
        private Text _journalText;
        [SerializeField]
        private Text _reanalysisText;
        [SerializeField]
        private Text _storagePathText;

        private readonly List<string> _journalFiles = new List<string>();

        private IJournalStore _journalStore;
        private JournalReanalysisService _reanalysisService;

        private void Awake()
        {
            _journalStore = _scenario.LoggerProvider.CreateStore();
            _reanalysisService = new JournalReanalysisService(_journalStore);
            _storagePathText.text =
                $"Journal directory: {GetCompactPath(_journalStore.StorageDirectory)}";
            BuildAnalyzerOptions();
            RefreshJournalFiles();
        }

        public void ShowCurrentJournal(SessionJournal journal)
        {
            _journalText.text = FormatJournal(journal);
        }

        public void RefreshJournalFiles()
        {
            _journalFiles.Clear();
            _journalFiles.AddRange(_journalStore.GetJournalFiles());

            _journalDropdown.ClearOptions();
            _journalDropdown.AddOptions(_journalFiles.Count == 0
                ? new List<string> { "No completed journals" }
                : _journalFiles
                    .Select((path, index) => GetJournalDisplayName(path, index))
                    .ToList());
            _journalDropdown.interactable = _journalFiles.Count > 0;
        }

        public void OpenSelectedJournal()
        {
            if (!TryGetSelectedJournalPath(out var path))
            {
                _journalText.text = "No completed journal is available.";
                return;
            }

            var loadResult = _journalStore.Load(path);
            _journalText.text = loadResult.Succeeded
                ? FormatJournal(loadResult.Journal)
                : $"Journal load failed:\n{loadResult.DiagnosticMessage}";
        }

        public void ReanalyzeSelectedJournal()
        {
            if (!TryGetSelectedJournalPath(out var path))
            {
                _reanalysisText.text = "Reanalysis: no completed journal is available.";
                return;
            }

            var providerIndex = Mathf.Clamp(
                _analyzerDropdown.value,
                0,
                _analyzerProviders.Count - 1);
            var result = _reanalysisService.Analyze(
                path,
                _analyzerProviders[providerIndex].CreateAnalyzer());
            _reanalysisText.text = FormatAnalysis(result);
        }

        private void BuildAnalyzerOptions()
        {
            _analyzerDropdown.ClearOptions();
            _analyzerDropdown.AddOptions(_analyzerProviders
                .Select(provider => provider.SupportedMode.ToString())
                .ToList());
        }

        private bool TryGetSelectedJournalPath(out string path)
        {
            if (_journalFiles.Count == 0)
            {
                path = string.Empty;
                return false;
            }

            var index = Mathf.Clamp(_journalDropdown.value, 0, _journalFiles.Count - 1);
            path = _journalFiles[index];
            return true;
        }

        private static string FormatJournal(SessionJournal journal)
        {
            var builder = new StringBuilder();
            builder.Append("Recent events — session ")
                .AppendLine(GetShortId(journal.SessionId));

            var stepNames = journal.Snapshot?.Steps
                .Where(step => step != null)
                .GroupBy(step => step.StepId)
                .ToDictionary(
                    group => group.Key,
                    group => string.IsNullOrWhiteSpace(group.First().DisplayName)
                        ? GetShortId(group.Key)
                        : group.First().DisplayName);

            foreach (var entry in journal.Events.Where(entry => entry != null).TakeLast(7))
            {
                builder.Append(FormatTime(entry.TimestampUtc))
                    .Append("  ")
                    .Append(entry.EventType);

                if (!string.IsNullOrWhiteSpace(entry.StepId))
                {
                    var stepName = stepNames != null &&
                                   stepNames.TryGetValue(entry.StepId, out var foundName)
                        ? foundName
                        : GetShortId(entry.StepId);
                    builder.Append(" | ").Append(stepName);
                }

                if (!string.IsNullOrWhiteSpace(entry.Result))
                {
                    builder.Append(" | ").Append(entry.Result);
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string GetJournalDisplayName(string path, int index)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.Length > 28)
            {
                fileName = fileName.Substring(0, 28) + "…";
            }

            return $"{index + 1}. {fileName}";
        }

        private static string GetCompactPath(string path)
        {
            var directoryName = Path.GetFileName(path);
            var parentName = Path.GetFileName(Path.GetDirectoryName(path));
            return string.IsNullOrWhiteSpace(parentName)
                ? directoryName
                : $"…/{parentName}/{directoryName}";
        }

        private static string GetShortId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= 8)
            {
                return value;
            }

            return value.Substring(0, 8);
        }

        private static string FormatTime(string timestampUtc)
        {
            return DateTimeOffset.TryParse(
                timestampUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var timestamp)
                ? timestamp.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture)
                : "--:--:--";
        }

        private static string FormatAnalysis(AnalysisResult result)
        {
            var compatibility = result.IsCompatible ? "compatible" : "incompatible";
            var warnings = result.Warnings.Count == 0
                ? string.Empty
                : "\nWarnings:\n" + string.Join("\n", result.Warnings);
            return $"Reanalysis ({result.AnalyzerId}, {compatibility}): " +
                   $"{result.Score}/{result.MaximumScore}\n{result.Summary}{warnings}";
        }
    }
}
