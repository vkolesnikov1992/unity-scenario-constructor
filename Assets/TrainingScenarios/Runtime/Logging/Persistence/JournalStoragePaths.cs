using System.IO;
using UnityEngine;

namespace TrainingScenarios.Runtime.Logging.Persistence
{
    public static class JournalStoragePaths
    {
        public static string DefaultDirectory => Path.Combine(
            Application.persistentDataPath,
            "TrainingScenarios",
            "Journals");
    }
}
