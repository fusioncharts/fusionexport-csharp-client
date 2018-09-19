using FusionCharts.FusionExport.Utils;
using Glob;
using HtmlAgilityPack;
using Ionic.Zip;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using static FusionCharts.FusionExport.Utils.Utils;


namespace FusionCharts.FusionExport.Client
{
    public class ExportConfig:IDisposable
    {
        const string CHARTCONFIG = "chartConfig";
        const string INPUTSVG = "inputSVG";
        const string CALLBACKS = "callbackFilePath";
        const string DASHBOARDLOGO = "dashboardlogo";
        const string OUTPUTFILEDEFINITION = "outputFileDefinition";
        const string CLIENTNAME = "clientName";
        const string TEMPLATE = "templateFilePath";
        const string RESOURCES = "resourceFilePath";        
        const string PLATFORM = "platform";      
        const string ASYNCCAPTURE = "asyncCapture";
        const string PAYLOAD = "payload";

        public class MetadataElementSchema
        {
            public enum ElementType
            {
                String,
                boolean,
                Integer
            };


            public enum Converter
            {
                PassThrough,
                BooleanConverter,
                NumberConverter,
            };


            public ElementType type;
            public Converter converter;
        }
        public class MetadataSchema : Dictionary<string, MetadataElementSchema>
        {
            public static MetadataSchema CreateFromMetaDataJSON()
            {
                var jsonContent = System.Text.Encoding.UTF8.GetString(Properties.Resources.metadataContent);
                return JsonConvert.DeserializeObject<MetadataSchema>(jsonContent);
            }

            public Dictionary<MetadataElementSchema.ElementType, Type> TypeMap = new Dictionary<MetadataElementSchema.ElementType, Type>()
            {
                {MetadataElementSchema.ElementType.String, typeof(string)},
                {MetadataElementSchema.ElementType.boolean, typeof(bool)},
                {MetadataElementSchema.ElementType.Integer, typeof(int)},
            };

            public Dictionary<MetadataElementSchema.Converter, Func<object, object>> ConverterMap = new Dictionary<MetadataElementSchema.Converter, Func<object, object>>()
            {
                {MetadataElementSchema.Converter.PassThrough, (object configValue) => configValue},
                {MetadataElementSchema.Converter.BooleanConverter, (object configValue) => BooleanFromStringNumber(configValue)},
                {MetadataElementSchema.Converter.NumberConverter, (object configValue) => NumberFromString(configValue)},
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
                        convertedValue = configValue;
                    }

                    return convertedValue;
                }
                throw new ArgumentException();
            }
        }

        public class ResourcePathInfo
        {
            public string internalPath { get; set; }
            public string externalPath { get; set; }
        }

        private class ResourcesSchema
        {
            public string basePath;

            public List<string> include, exclude;

            public string resolvePath;
        }

        private MetadataSchema metadata;
        private Dictionary<string, object> configs;
        private bool enableTypeCheckAndConversion;

        public void Dispose()
        {
            if (metadata != null)
            {
                metadata.Clear();
                metadata = null;
            }

            if (configs != null)
            {
                this.Clear();
                configs = null;
            }
        }

        public ExportConfig(bool enableTypeCheckAndConversion = true)
        {
            this.enableTypeCheckAndConversion = enableTypeCheckAndConversion;

            this.metadata = MetadataSchema.CreateFromMetaDataJSON();
            this.configs = new Dictionary<string, object>();
        }

        public void Set(string configName, object configValue)
        {
            if (enableTypeCheckAndConversion)
            {
                configValue = this.metadata.TryConvertType(configName, configValue);
                this.metadata.CheckType(configName, configValue);
            }

            configs[configName] = configValue;
        }

        public object Get(string configName)
        {
            return configs[configName];
        }

        public object GetValueOrDefault(string key, object defaultValue)
        {
            return this.configs.GetValueOrDefault(key, defaultValue);
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
            if (this.configs != null && this.configs.Count > 0)
            {
                if (this.configs.ContainsKey(PAYLOAD))
                {
                    string payloadFilePath = this.configs[PAYLOAD].ToString();
                    Utils.Utils.DeleteFile(payloadFilePath);
                }

                this.configs.Clear();
            }
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

        public MultipartFormDataContent GetFormattedConfigs()
        {
            MultipartFormDataContent formDataContent = new MultipartFormDataContent();

            using (ExportConfig clonedSelf = this.CloneWithProcessedProperties())
            {
                foreach (var config in clonedSelf.configs)
                {
                    if (config.Key.Equals(ExportConfig.PAYLOAD))
                    {
                        using (StreamContent streamContent = new StreamContent(File.Open(config.Value.ToString(), FileMode.Open)))
                        {
                            formDataContent.Add(Utils.Utils.CloneStreamContent(streamContent), config.Key, "file");                            
                        }
                    }
                    else
                    {
                        formDataContent.Add(new StringContent(config.Value.ToString().Replace("\n", "").Replace("\r", "")), config.Key);
                    }
                }
            }
            return formDataContent;
        }

        public ExportConfig CloneWithProcessedProperties()
        {
            string internalFilePath, oldValue;

            List<ResourcePathInfo> zipBag = new List<ResourcePathInfo>();

            var selfClone = this.Clone();
            selfClone.enableTypeCheckAndConversion = false;

            selfClone.Set(CLIENTNAME, "C#");
            selfClone.Set(PLATFORM, Environment.OSVersion.Platform.ToString());

            if (selfClone.Has(CHARTCONFIG))
            {
                oldValue = (string)selfClone.Get(CHARTCONFIG);
                selfClone.Remove(CHARTCONFIG);
                
                if (oldValue.EndsWith(".json"))
                {
                    oldValue = ReadFileContent(oldValue, encodeBase64: false);
                }

                selfClone.Set(CHARTCONFIG, oldValue);
            }

            if (selfClone.Has(INPUTSVG))
            {
                oldValue = (string)selfClone.Get(INPUTSVG);
                selfClone.Remove(INPUTSVG);

                internalFilePath = "inputSVG.svg";
                zipBag.Add(new ResourcePathInfo()
                {
                     internalPath = internalFilePath,
                     externalPath = oldValue
                });

                selfClone.Set(INPUTSVG, internalFilePath);                
            }

            if (selfClone.Has(CALLBACKS))
            {
                oldValue = (string)selfClone.Get(CALLBACKS);
                selfClone.Remove(CALLBACKS);
                
                internalFilePath = "callbackFile.js";
                zipBag.Add(new ResourcePathInfo()
                {
                    internalPath = internalFilePath,
                    externalPath = oldValue
                });

                selfClone.Set(CALLBACKS, internalFilePath);
            }

            if (selfClone.Has(DASHBOARDLOGO))
            {
                oldValue = (string)selfClone.Get(DASHBOARDLOGO);
                selfClone.Remove(DASHBOARDLOGO);
                
                var ext = new FileInfo(oldValue).Extension;
                internalFilePath = string.Format("dashboardLogo{0}", ext.StartsWith(".")? ext: "." + ext);
                zipBag.Add(new ResourcePathInfo()
                {
                    internalPath = internalFilePath,
                    externalPath = oldValue
                });

                selfClone.Set(DASHBOARDLOGO, internalFilePath);                
            }

            if (selfClone.Has(OUTPUTFILEDEFINITION))
            {
                oldValue = (string)selfClone.Get(OUTPUTFILEDEFINITION);
                selfClone.Remove(OUTPUTFILEDEFINITION);

                selfClone.Set(OUTPUTFILEDEFINITION, ReadFileContent(oldValue, encodeBase64: false));
            }          

            if (selfClone.Has(TEMPLATE))
            {
                string templatePathWithinZip;
                List<ResourcePathInfo> zipPaths;
                selfClone.createTemplateZipPaths(out zipPaths, out templatePathWithinZip);
                selfClone.Set(TEMPLATE, templatePathWithinZip);
                zipBag.AddRange(zipPaths);                
            }

            if (selfClone.Has(ASYNCCAPTURE))
            {
                oldValue = (string)selfClone.Get(ASYNCCAPTURE);
                selfClone.Remove(ASYNCCAPTURE);

                if (!string.IsNullOrEmpty(oldValue))
                {
                    if (Convert.ToBoolean(oldValue))
                    {
                        selfClone.Set(ASYNCCAPTURE, true);
                    }
                }
            }

            if (zipBag.Count > 0)
            {
                string zipFile = ExportConfig.generateZip(zipBag);
                selfClone.Set(PAYLOAD, zipFile);
            }
            zipBag.Clear();

            return selfClone;
        }

        private void createTemplateZipPaths(out List<ResourcePathInfo> outZipPaths, out string outTemplatePathWithinZip)
        {
            List<string> listExtractedPaths = findResources();
            List<string> listResourcePaths;
            string baseDirectoryPath;
            this.resolveResourceGlobFiles(out baseDirectoryPath, out listResourcePaths);
            string templateFilePath = (string)this.Get(TEMPLATE);
            templateFilePath = Path.GetFullPath(templateFilePath);

            if (baseDirectoryPath == null || string.IsNullOrEmpty(baseDirectoryPath))
            {
                var listExtractedPathsPlusTemplate = new List<string>();
                listExtractedPathsPlusTemplate.AddRange(listExtractedPaths);
                listExtractedPathsPlusTemplate.Add(templateFilePath);

                var commonDirectoryPath = GetCommonAncestorDirectory(listExtractedPathsPlusTemplate.ToArray());

                if (!string.IsNullOrEmpty(commonDirectoryPath))
                {
                    baseDirectoryPath = commonDirectoryPath;
                }
                else
                {
                    throw new DirectoryNotFoundException("All the extracted resources and template might not be in the same drive");
                }
            }
                        
            // Filter listResourcePaths to those only which are within basePath
            listResourcePaths = listResourcePaths.Where((tmpPath) => IsWithinPath(tmpPath, baseDirectoryPath)).ToList();
            
            // Make map relative version of extracted and resource file paths (compared to basepath) with original filepath
            var mapExtractedPathAbsToRel = new Dictionary<string, string>();
            foreach (var tmpPath in listExtractedPaths)
            {
                mapExtractedPathAbsToRel[tmpPath] = GetRelativePathFrom(tmpPath, baseDirectoryPath);
            }

            //var mapResourcePathAbsToRel = new Dictionary<string, string>();
            foreach (var tmpPath in listResourcePaths)
            {
                mapExtractedPathAbsToRel[tmpPath] = GetRelativePathFrom(tmpPath, baseDirectoryPath);
            }

            var templateFilePathWithinZipRel = GetRelativePathFrom(templateFilePath, baseDirectoryPath);

            mapExtractedPathAbsToRel.Add(templateFilePath, templateFilePathWithinZipRel);

            List<ResourcePathInfo> zipPaths = generatePathForZip(mapExtractedPathAbsToRel, baseDirectoryPath);

            foreach(var zipPath in zipPaths)
            {
                zipPath.internalPath = Path.Combine("template", zipPath.internalPath);
            }

            string templatePathWithinZip = Path.Combine(
              "template",
              GetRelativePathFrom(templateFilePath, baseDirectoryPath)
            );

            outZipPaths = zipPaths;
            outTemplatePathWithinZip = templatePathWithinZip;
            ///CreateBase64ZippedTemplate(out outZipPaths, out outTemplatePathWithinZip);
        }

        private List<string> findResources()
        {
            string templateFilePath = (string)this.Get(TEMPLATE);
            
            if (!string.IsNullOrEmpty(templateFilePath))
            {
                string templateDirectory = Path.GetDirectoryName(templateFilePath);
                templateFilePath = Path.GetFullPath(templateFilePath);

                // Load templateFilePath content as html page
                var htmlDoc = new HtmlDocument();
                htmlDoc.Load(templateFilePath);
               
                List<string> listExtractedPaths = new List<string>();

                // Find all link, script, img tags with local URL from loaded html
                var filteredLinkTags = EmptyListIfNull(htmlDoc.DocumentNode
                    .SelectNodes("//link"))
                    .Where((linkTag) => linkTag.HasAttributes && linkTag.Attributes["href"] != null && isLocalResource(linkTag.Attributes["href"].Value));

                var filteredScriptTags = EmptyListIfNull(htmlDoc.DocumentNode
                   .SelectNodes("//script"))
                .Where(scriptTag => scriptTag.HasAttributes && scriptTag.Attributes["src"] != null && isLocalResource(scriptTag.Attributes["src"].Value));

                var filteredImageTags = EmptyListIfNull(htmlDoc.DocumentNode
                    .SelectNodes("//img"))
                    .Where((imageTag) => imageTag.HasAttributes && imageTag.Attributes["src"] != null && isLocalResource(imageTag.Attributes["src"].Value));

                // Fot these filtered link, script, img tags - map their full resolved filepath to a tempfilename 
                // which will be used within zip and change the URLs (within html) to this tempfilename.
                foreach (var linkTag in filteredLinkTags)
                {
                    var originalFilePath = linkTag.Attributes["href"].Value;

                    var resolvedFilePath = GetFullPathWrtBasePath(originalFilePath, Path.GetDirectoryName(templateFilePath));
                    listExtractedPaths.Add(resolvedFilePath);
                }

                foreach (var scriptTag in filteredScriptTags)
                {
                    var originalFilePath = scriptTag.Attributes["src"].Value;

                    var resolvedFilePath = GetFullPathWrtBasePath(originalFilePath, Path.GetDirectoryName(templateFilePath));
                    listExtractedPaths.Add(resolvedFilePath);
                }

                foreach (var imageTag in filteredImageTags)
                {
                    var originalFilePath = imageTag.Attributes["src"].Value;

                    var resolvedFilePath = GetFullPathWrtBasePath(originalFilePath, Path.GetDirectoryName(templateFilePath));
                    listExtractedPaths.Add(resolvedFilePath);
                }

                return listExtractedPaths;
            }
            return new List<string>();
        }

        private void resolveResourceGlobFiles(out string outBaseDirectoryPath, out List<string> outListResourcePaths)
        {            
            string baseDirectoryPath = null;
            List<string> listResourcePaths = new List<string>();
            
            if (!this.Has(RESOURCES))
            {
                outBaseDirectoryPath = baseDirectoryPath;
                outListResourcePaths = listResourcePaths;
                return;
            }

            string resourceFilePath = (string)this.Get(RESOURCES);
            resourceFilePath = Path.GetFullPath(resourceFilePath);

            string resourceDirectoryPath = Path.GetDirectoryName(resourceFilePath);

            // Load resourceFilePath content (JSON) as instance of Resources
            var resources = JsonConvert.DeserializeObject<ResourcesSchema>(File.ReadAllText(resourceFilePath));

            if (resources.include == null)
                resources.include = new List<string>();

            if (resources.exclude == null)
                resources.exclude = new List<string>();
            
            // New attribute `resolvePath` - overloads actual direcotry location for glob resolve
            if (resources.resolvePath != null)
            {
                resourceDirectoryPath = resources.resolvePath;
            }

            {
                var listResourceIncludePaths = new List<string>();
                var listResourceExcludePaths = new List<string>();

                var root = new DirectoryInfo(resourceDirectoryPath);

                /* eslint-disable no-restricted-syntax */
                foreach (var eachIncludePath in resources.include)
                {
                    var matchedFiles = root.GlobFiles(eachIncludePath)
                                .Select((fileInfo) => fileInfo.FullName)
                                .ToList();
                    listResourceIncludePaths.AddRange(matchedFiles);
                }

                foreach (var eachExcludePath in resources.exclude)
                {
                    var matchedFiles = root.GlobFiles(eachExcludePath)
                                .Select((fileInfo) => fileInfo.FullName)
                                .ToList();
                    listResourceExcludePaths.AddRange(matchedFiles);                    
                }
                /* eslint-enable no-restricted-syntax */

                listResourcePaths = listResourceIncludePaths.Except(listResourceExcludePaths).ToList();
                baseDirectoryPath = resources.basePath;
            }

            outBaseDirectoryPath = baseDirectoryPath;
            outListResourcePaths = listResourcePaths;
        }

        private List<ResourcePathInfo> generatePathForZip(Dictionary<string, string> listAllFilePaths, string baseDirectoryPath)
        {
            List<ResourcePathInfo> listFilePath = new List<ResourcePathInfo>();

            foreach (KeyValuePair<string,string> filepath in listAllFilePaths)
            {
                string filePathWithinZip = GetRelativePathFrom(filepath.Key, baseDirectoryPath);
                listFilePath.Add(new ResourcePathInfo()
                {
                     internalPath = filePathWithinZip,
                     externalPath = GetAbsolutePathFrom(filepath.Key)
                });
            }
            return listFilePath;
        }

        private static string generateZip(List<ResourcePathInfo> fileBag)
        {
            var tempZipFilePath = GetTempFileName();
            using (ZipFile zip = new ZipFile())
            {
                foreach (var file in fileBag)
                {
                    zip.AddFile(file.externalPath, Path.GetDirectoryName(file.internalPath));                
                }
                zip.Save(tempZipFilePath);
            }
            return tempZipFilePath;
        }        
    }
}
