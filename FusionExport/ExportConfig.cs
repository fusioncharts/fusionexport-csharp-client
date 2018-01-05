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
        public class MetadataElementSchema
        {
            public enum ElementType
            {
                String,
                boolean,
                integer
            };

            public enum Converter
            {
                BooleanFromStringNumber,
                NumberFromString
            };


            public ElementType type;
            public Converter converter;
        }
        public class MetadataSchema : Dictionary<string, MetadataElementSchema>
        {

            public static MetadataSchema CreateFromMetaDataJSON()
            {
                var jsonContent = File.ReadAllText("../metadata/export-sdk-metadata.json");
                return JsonConvert.DeserializeObject<MetadataSchema>(jsonContent);
            }

            public Dictionary<MetadataElementSchema.ElementType, Type> TypeMap = new Dictionary<MetadataElementSchema.ElementType, Type>()
            {
                {MetadataElementSchema.ElementType.String, typeof(string)},
                {MetadataElementSchema.ElementType.boolean, typeof(bool)},
                {MetadataElementSchema.ElementType.integer, typeof(int)},
            };

            public Dictionary<MetadataElementSchema.Converter, Func<object, object>> ConverterMap = new Dictionary<MetadataElementSchema.Converter, Func<object, object>>()
            {
                {MetadataElementSchema.Converter.BooleanFromStringNumber, (object configValue) => BooleanFromStringNumber(configValue)},
                {MetadataElementSchema.Converter.NumberFromString, (object configValue) => NumberFromString(configValue)},
            };

            public static bool BooleanFromStringNumber(object configValue)
            {
                if (configValue.GetType() == typeof(bool))
                {
                    return (bool)configValue;
                }
                else if (configValue.GetType() == typeof(string))
                {
                    string value = (string)configValue;
                    value = value.ToLower();

                    if ((value == "true") || (value == "1"))
                    {
                        return true;
                    }
                    else if ((value == "false") || (value == "0"))
                    {
                        return false;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }
                else if (configValue.GetType() == typeof(int))
                {
                    int value = (int)configValue;

                    if (value == 1)
                    {
                        return true;
                    }
                    else if (value == 0)
                    {
                        return false;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    throw new InvalidCastException();
                }
            }

            public static int NumberFromString(object configValue)
            {
                if (configValue.GetType() == typeof(int))
                {
                    return (int)configValue;
                }
                else if (configValue.GetType() == typeof(string))
                {
                    string value = (string)configValue;

                    return Int32.Parse(value);
                }
                else
                {
                    throw new InvalidCastException();
                }
            }

            public void CheckType(string configName, object configValue)
            {
                if (this.ContainsKey(configName))
                {
                    var metadataElement = this[configName];

                    var expectedType = TypeMap[metadataElement.type];

                    if (configValue.GetType() != expectedType)
                    {
                        throw new ArgumentException();
                    }
                }
                else
                {
                    throw new ArgumentException();
                }
            }

            public object TryConvertType(string configName, object configValue)
            {
                if (this.ContainsKey(configName))
                {
                    var metadataElement = this[configName];

                    object convertedValue;
                    if (ConverterMap.ContainsKey(metadataElement.converter))
                    {
                        convertedValue = ConverterMap[metadataElement.converter](configValue);
                    }
                    else
                    {
                        convertedValue = configValue
                    }

                    return convertedValue;
                }
                throw new ArgumentException();
            }
        }



        private MetadataSchema metadata;
        private Dictionary<string, object> configs;

        public ExportConfig()
        {
            this.metadata = MetadataSchema.CreateFromMetaDataJSON();
            this.configs = new Dictionary<string, object>();
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

        public void Set(string configName, object configValue)
        {
            configName = configName.ToLower();

            configValue = this.metadata.TryConvertType(configName, configValue);
            this.metadata.CheckType(configName, configValue);

            configs[configName] = configValue;
        }

        public object Get(string configName)
        {
            configName = configName.ToLower();

            return configs[configName];
        }

        public bool Remove(string configName)
        {
            configName = configName.ToLower();

            return this.configs.Remove(configName);
        }

        public bool Has(string configName)
        {
            configName = configName.ToLower();

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

        public object[] ConfigValues()
        {
            List<object> configValues = new List<object>();
            foreach (var value in this.configs.Values)
            {
                configValues.Add(value);
            }
            return configValues.ToArray();
        }

        public ExportConfig Clone()
        {
            ExportConfig newExportConfig = new ExportConfig();
            foreach (KeyValuePair<string, object> config in this.configs)
            {
                newExportConfig.Set(config.Key, config.Value);
            }
            return newExportConfig;
        }

        public string GetFormattedConfigs()
        {
            this.ProcessProperties();
            return JsonConvert.SerializeObject(this.configs);
        }

        public void ProcessProperties()
        {
            var selfClone = this.Clone();

            const string INPUTSVG = "inputSVG";
            const string CALLBACKS = "callbacks";
            const string DASHBOARDLOGO = "dashboardlogo";
            const string OUTPUTFILE = "outputFile";

            if (this.Has(INPUTSVG))
            {
                var oldValue = (string)this.Get(INPUTSVG);
                this.Remove(INPUTSVG);

                this.Set(INPUTSVG, ReadFileContent(oldValue, encodeBase64:true));
            }

            if (this.Has(CALLBACKS))
            {
                var oldValue = (string)this.Get(CALLBACKS);
                this.Remove(CALLBACKS);

                this.Set(CALLBACKS, ReadFileContent(oldValue, encodeBase64: true));
            }

            if (this.Has(DASHBOARDLOGO))
            {
                var oldValue = (string)this.Get(DASHBOARDLOGO);
                this.Remove(DASHBOARDLOGO);

                this.Set(DASHBOARDLOGO, ReadFileContent(oldValue, encodeBase64: true));
            }

            if (this.Has(DASHBOARDLOGO))
            {
                var oldValue = (string)this.Get(DASHBOARDLOGO);
                this.Remove(DASHBOARDLOGO);

                this.Set(DASHBOARDLOGO, ReadFileContent(oldValue, encodeBase64: true));
            }

            if (this.Has(OUTPUTFILE))
            {
                var oldValue = (string)this.Get(OUTPUTFILE);
                this.Remove(OUTPUTFILE);

                this.Set(OUTPUTFILE, ReadFileContent(oldValue, encodeBase64: false));
            }

            this._templateZipBase64Content = this.CreateBase64ZippedTemplate();
        }

        private static string ReadFileContent(string potentiallyFilePath, bool encodeBase64 = false)
        {
            if (isLocalResource(potentiallyFilePath))
            {
                try
                {
                    string content;
                    if (encodeBase64)
                    {
                        Byte[] bytes = File.ReadAllBytes(potentiallyFilePath);
                        content = Convert.ToBase64String(bytes);
                    }
                    else
                    {
                        content = File.ReadAllText(potentiallyFilePath);
                    }
                    return content;
                }
                catch (Exception ex)
                {
                    if (
                        (ex is PathTooLongException) ||
                        (ex is DirectoryNotFoundException) ||
                        (ex is IOException) ||
                        (ex is FileNotFoundException)
                        )
                    {
                        return potentiallyFilePath;
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
            else
            {
                return potentiallyFilePath;
            }

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
            return ReadFileContent(tempZipFilePath);

        }

    }
}
