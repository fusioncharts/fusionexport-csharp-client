using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using Ionic.Zip;

namespace FusionCharts.FusionExport.Client
{
    public class ExportConfig
    {
        [JsonObject(MemberSerialization.OptIn)]
        public class ConfigSchema
        {
            // Schema Properties
            [JsonProperty]
            public const string clientName = "C#";

            [JsonProperty]
            public Dictionary<string, object> chartConfig;

            [JsonProperty("inputSVG")]
            public string _inputSVGBase64Content;
            public string inputSVG;

            [JsonProperty("templateFilePath")]
            public string _templateZipBase64Content;
            public string templateFilePath;
            public string resourceFilePath;

            [JsonProperty("callbackFilePath")]
            public string _callbackBase64Content;
            public string callbackFilePath;

            [JsonProperty]
            public bool asyncCapture;

            [JsonProperty]
            public double maxWaitForCaptureExit;

            [JsonProperty("dashboardLogo")]
            public string _dashboardLogoBase64Content;
            public string dashboardLogo;

            [JsonProperty]
            public string dashboardHeading;

            [JsonProperty]
            public string dashboardSubheading;

            [JsonProperty("outputFileDefinition")]
            public string _outputFileDefinitionContent;
            public string outputFileDefinition;

            [JsonProperty]
            public string type;

            [JsonProperty]
            public string exportFile;

            [JsonProperty]
            public bool exportAsZip;

            private class Resources
            {
                public List<string> images = new List<string>() { };
                public List<string> stylesheets = new List<string>() { };
                public List<string> javascripts = new List<string>() { };
            }

            public void ProcessProperties()
            {
                this._inputSVGBase64Content = EncodeFileContentToBase64(this.inputSVG);
                this._callbackBase64Content = EncodeFileContentToBase64(this.callbackFilePath);
                this._dashboardLogoBase64Content = EncodeFileContentToBase64(this.dashboardLogo);

                this._outputFileDefinitionContent = File.ReadAllText(this.outputFileDefinition);

                this._templateZipBase64Content = this.CreateBase64ZippedTemplate();
            }

            public string DeSerializeToJSON()
            {
                return JsonConvert.SerializeObject(this);
            }

            private static string EncodeFileContentToBase64(string filePath)
            {
                Byte[] bytes = File.ReadAllBytes(filePath);
                String base64Content = Convert.ToBase64String(bytes);

                return base64Content;
            }

            private string CreateBase64ZippedTemplate()
            {
                // Load templateFilePath content as html page
                var htmlDoc = new HtmlDocument();
                htmlDoc.Load(this.templateFilePath);

                // Load resourceFilePath content (JSON) as instance of Resources
                var resources = JsonConvert.DeserializeObject<Resources>(File.ReadAllText(resourceFilePath));

                // Create map of (filepath within zip folder) : (filepath of original file)
                var mapOriginalFilePathToZipPath = new Dictionary<string, string>();

                // Find all link, script, img tags with local URL from loaded html
                var filteredLinkTags = htmlDoc.DocumentNode
                    .SelectNodes("//link")
                    .Where((linkTag) => isLocalResource(linkTag.Attributes["href"].Value));

                var filteredScriptTags = htmlDoc.DocumentNode
                    .SelectNodes("//script")
                    .Where((scriptTag) => isLocalResource(scriptTag.Attributes["src"].Value));

                var filteredImageTags = htmlDoc.DocumentNode
                    .SelectNodes("//img")
                    .Where((imageTag) => isLocalResource(imageTag.Attributes["src"].Value));

                // Fot these filtered link, script, img tags - map their full resolved filepath to a tempfilename 
                // which will be used within zip and change the URLs (within html) to this tempfilename.
                foreach (var linkTag in filteredLinkTags)
                {
                    var originalFilePath = linkTag.Attributes["href"].Value;
                    var withinZipFileName = GetRandomFileNameWithExtension(originalFilePath);

                    var resolvedOriginalFilePath = GetFullPathWrtBasePath(originalFilePath, Path.GetDirectoryName(templateFilePath));
                    mapOriginalFilePathToZipPath[resolvedOriginalFilePath] = withinZipFileName;

                    linkTag.Attributes["href"].Value = withinZipFileName;
                }

                foreach (var scriptTag in filteredScriptTags)
                {
                    var originalFilePath = scriptTag.Attributes["src"].Value;
                    var withinZipFileName = GetRandomFileNameWithExtension(originalFilePath);

                    var resolvedOriginalFilePath = GetFullPathWrtBasePath(originalFilePath, Path.GetDirectoryName(templateFilePath));
                    mapOriginalFilePathToZipPath[resolvedOriginalFilePath] = withinZipFileName;

                    scriptTag.Attributes["href"].Value = withinZipFileName;
                }

                foreach (var imageTag in filteredImageTags)
                {
                    var originalFilePath = imageTag.Attributes["src"].Value;
                    var withinZipFileName = GetRandomFileNameWithExtension(originalFilePath);

                    var resolvedOriginalFilePath = GetFullPathWrtBasePath(originalFilePath, Path.GetDirectoryName(templateFilePath));
                    mapOriginalFilePathToZipPath[resolvedOriginalFilePath] = withinZipFileName;

                    imageTag.Attributes["href"].Value = withinZipFileName;
                }

                // Put the modified template file + extracted resources from html + provided resource files in a temp folder
                var tempZipDirectoryPath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

                {
                    // Write modified template file
                    var htmlContent = htmlDoc.DocumentNode.OuterHtml;
                    var templateWithinZipFullPath = Path.Combine(tempZipDirectoryPath, "template.html");

                    File.WriteAllText(templateWithinZipFullPath, htmlContent);
                }
                {
                    // Write extracted resources from html 
                    foreach (var eachMap in mapOriginalFilePathToZipPath)
                    {
                        var originalFilePath = eachMap.Key;
                        var resourceWithinZipFullPath = GetFullPathWrtBasePath(eachMap.Value, tempZipDirectoryPath);

                        File.Copy(originalFilePath, resourceWithinZipFullPath);
                    }
                }
                {
                    // Write provided resource files

                    // All resource file path must be relative path
                    foreach (var resourceList in new List<List<string>>() { resources.stylesheets, resources.javascripts, resources.images })
                    {
                        foreach (var resourceRelativePath in resourceList)
                        {
                            var sourceFullPath = GetFullPathWrtBasePath(resourceRelativePath, Path.GetDirectoryName(templateFilePath));
                            var destinationFilePath = GetFullPathWrtBasePath(resourceRelativePath, tempZipDirectoryPath);

                            File.Copy(sourceFullPath, destinationFilePath);
                        }
                    }
                }

                // Zip the folder
                var tempZipFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
                CreateZipFromDirectory(tempZipDirectoryPath, tempZipFilePath);

                // Set this zip content to _templateZipBase64Content
                return EncodeFileContentToBase64(tempZipFilePath);

            }

            private static void CreateZipFromDirectory(string sourceFolderPath, string destinationZipFolder)
            {
                using (ZipFile zip = new ZipFile())
                {
                    string[] files = Directory.GetFiles(sourceFolderPath);
                    zip.AddFiles(files, ".");
                    zip.Save(destinationZipFolder);
                }
            }
        }

        public static string GetFullPathWrtBasePath(string extraPath, string basePath)
        {
            if (Path.GetFullPath(extraPath) == extraPath)
            {
                return extraPath;
            }
            else
            {
                return Path.Combine(basePath, extraPath);
            }
        }

        public static string GetRandomFileNameWithExtension(string targetExtension)
        {
            return String.Join("",
                new string[] {
                        Path.GetRandomFileName(),
                        Path.GetExtension(targetExtension) });
        }

        private static bool isLocalResource(string testResourceFilePath)
        {
            Regex remoteResourcePattern = new Regex(@"^http(s)?:\/\/");
            return !remoteResourcePattern.IsMatch(testResourceFilePath.Trim());
        }

        public string ToJSONString()
        {
            var configSchema = new ConfigSchema();
            ConfigSchema.ProcessProperties();

            return configSchema.DeSerializeToJSON();
        }

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
            if (configsAsJSON.Length >= 2)
            {
                configsAsJSON.Remove(configsAsJSON.Length - 2, 2); // remove last comma and space characters
            }
            configsAsJSON.Insert(0, "{ ");
            configsAsJSON.Append(" }");
            return configsAsJSON.ToString();
        }

        private string GetFormattedConfigValue(string configName, string configValue)
        {
            switch (configName)
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
