using System.Collections.Generic;
using TrainingScenarios.Runtime.Logging.Models;
using TrainingScenarios.Runtime.Logging.Persistence;

namespace TrainingScenarios.Runtime.Logging.Contracts
{
    public interface IJournalStore
    {
        string StorageDirectory { get; }

        JournalSaveResult Save(SessionJournal journal);

        JournalLoadResult Load(string filePath);

        IReadOnlyList<string> GetJournalFiles();
    }
}
