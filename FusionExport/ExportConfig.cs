using System;
using System.Collections.Generic;
using System.Text;

namespace FusionCharts.FusionExport.Client
{
    public class ExportConfig
    {
        private Dictionary<string, string> configs;

        public ExportConfig()
        {
            this.configs = new Dictionary<string, string>();
        }

        public void Set(string configName, string configValue)
        {
            configs[configName] = configValue;
        }

        public string Get(string configName)
        {
            return configs[configName];
        }

        public void Clear()
        {
            this.configs.Clear();
        }

        public string GetFormattedConfigs()
        {
            StringBuilder configsAsJSON = new StringBuilder();
            foreach (KeyValuePair<string, string> config in this.configs)
            {
                string formattedConfigValue = this.GetFormattedConfigValue(config.Key, config.Value);
                string keyValuePair = String.Format("\"{0}\": {1}, ", config.Key, formattedConfigValue);
                configsAsJSON.Append(keyValuePair);
            }
            configsAsJSON.Remove(configsAsJSON.Length - 2, 2); // remove last comma and space characters
            configsAsJSON.Insert(0, "{ ");
            configsAsJSON.Append(" }");
            return configsAsJSON.ToString();
        }

        private string GetFormattedConfigValue(string configName, string configValue)
        {
            switch(configName)
            {
                case "chartConfig":
                    return configValue;
                default:
                    return String.Format("\"{0}\"", configValue);
            }
        }
    }
}
