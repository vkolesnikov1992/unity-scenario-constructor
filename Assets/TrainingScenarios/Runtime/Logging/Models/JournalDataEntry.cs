using System.Runtime.Serialization;

namespace TrainingScenarios.Runtime.Logging.Models
{
    [DataContract]
    public sealed class JournalDataEntry
    {
        [DataMember(Name = "key", Order = 1)]
        private string _key;

        [DataMember(Name = "value", Order = 2)]
        private string _value;

        public JournalDataEntry(string key, string value)
        {
            _key = key;
            _value = value;
        }

        public string Key => _key;

        public string Value => _value;
    }
}
