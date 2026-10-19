using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using TrainingScenarios.Runtime.Logging.Contracts;
using TrainingScenarios.Runtime.Logging.Models;

namespace TrainingScenarios.Runtime.Logging.Persistence
{
    public sealed class JsonJournalStore : IJournalStore
    {
        private readonly DataContractJsonSerializer _serializer =
            new DataContractJsonSerializer(typeof(SessionJournal));

        public JsonJournalStore()
            : this(JournalStoragePaths.DefaultDirectory)
        {
        }

        public JsonJournalStore(string storageDirectory)
        {
            StorageDirectory = storageDirectory;
        }

        public string StorageDirectory { get; }

        public JournalSaveResult Save(SessionJournal journal)
        {
            if (!journal.IsCompleted)
            {
                return new JournalSaveResult(
                    false,
                    string.Empty,
                    "Only completed session journals can be saved.");
            }

            var fileName = $"{MakeFileNameSafe(journal.ScenarioId)}_{journal.SessionId}.json";
            var filePath = Path.Combine(StorageDirectory, fileName);

            try
            {
                Directory.CreateDirectory(StorageDirectory);

                using (var memoryStream = new MemoryStream())
                {
                    _serializer.WriteObject(memoryStream, journal);
                    var json = Encoding.UTF8.GetString(memoryStream.ToArray());
                    File.WriteAllText(filePath, json, new UTF8Encoding(false));
                }

                return new JournalSaveResult(true, filePath, string.Empty);
            }
            catch (Exception exception) when (IsPersistenceException(exception))
            {
                return new JournalSaveResult(false, filePath, exception.Message);
            }
        }

        public JournalLoadResult Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new JournalLoadResult(
                    false,
                    null,
                    $"Journal file was not found: {filePath}");
            }

            try
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var journal = _serializer.ReadObject(stream) as SessionJournal;
                    if (journal == null)
                    {
                        return new JournalLoadResult(
                            false,
                            null,
                            "The file does not contain a session journal.");
                    }

                    return new JournalLoadResult(true, journal, string.Empty);
                }
            }
            catch (Exception exception) when (IsPersistenceException(exception))
            {
                return new JournalLoadResult(false, null, exception.Message);
            }
        }

        public IReadOnlyList<string> GetJournalFiles()
        {
            if (!Directory.Exists(StorageDirectory))
            {
                return Array.Empty<string>();
            }

            try
            {
                return Directory
                    .GetFiles(StorageDirectory, "*.json", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .ToArray();
            }
            catch (Exception exception) when (IsPersistenceException(exception))
            {
                return Array.Empty<string>();
            }
        }

        private static string MakeFileNameSafe(string value)
        {
            var invalidCharacters = Path.GetInvalidFileNameChars();
            var result = value;

            foreach (var character in invalidCharacters)
            {
                result = result.Replace(character, '_');
            }

            return result;
        }

        private static bool IsPersistenceException(Exception exception)
        {
            return exception is IOException ||
                   exception is UnauthorizedAccessException ||
                   exception is SerializationException ||
                   exception is XmlException ||
                   exception is ArgumentException ||
                   exception is NotSupportedException;
        }
    }
}
