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

        public bool Remove(string configName)
        {
            return this.configs.Remove(configName);
        }

        public bool Has(string configName)
        {
            return this.configs.ContainsKey(configName);
        }

        public void Clear()
        {
            this.configs.Clear();
        }

        public int Count
        {
            get { return this.configs.Count; }
        }
        
        public string[] ConfigNames()
        {
            List<string> configNames = new List<string>();
            foreach (string key in this.configs.Keys)
            {
                configNames.Add(key);
            }
            return configNames.ToArray();
        }

        public string[] ConfigValues()
        {
            List<string> configValues = new List<string>();
            foreach (string value in this.configs.Values)
            {
                configValues.Add(value);
            }
            return configValues.ToArray();
        }

        public ExportConfig Clone()
        {
            ExportConfig newExportConfig = new ExportConfig();
            foreach (KeyValuePair<string, string> config in this.configs)
            {
                newExportConfig.Set(config.Key, config.Value);
            }
            return newExportConfig;
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
            if(configsAsJSON.Length >= 2)
            {
                configsAsJSON.Remove(configsAsJSON.Length - 2, 2); // remove last comma and space characters
            }
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
                case "maxWaitForCaptureExit":
                    return configValue;
                case "asyncCapture":
                    return configValue.ToLower();
                case "exportAsZip":
                    return configValue.ToLower();
                default:
                    return String.Format("\"{0}\"", configValue);
            }
        }
    }
}
