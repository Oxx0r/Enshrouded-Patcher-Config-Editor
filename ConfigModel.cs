using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EnshroudedConfigManager
{
    public class ConfigRoot
    {
        public string gameDirectory { get; set; } = "";
        public string outputDirectory { get; set; } = "";

        public JObject player { get; set; } = new JObject();
        public JObject inventory { get; set; } = new JObject();
        public JObject world { get; set; } = new JObject();
        public JObject gameplay { get; set; } = new JObject();

        public Dictionary<string, bool> settings { get; set; } = new Dictionary<string, bool>();
        public string kfcParserVersion { get; set; } = "";
    }
}