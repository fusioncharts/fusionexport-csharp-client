using FusionCharts.FusionExport.Utils;
using Glob;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static FusionCharts.FusionExport.Utils.Utils;


namespace FusionCharts.FusionExport.Client
{
    public class ExportConfig
    {
        const string CHARTCONFIG = "chartConfig";
        const string INPUTSVG = "inputSVG";
        const string CALLBACKS = "callbackFilePath";
        const string DASHBOARDLOGO = "dashboardlogo";
        const string OUTPUTFILEDEFINITION = "outputFileDefinition";
        const string CLIENTNAME = "clientName";
        const string TEMPLATE = "templateFilePath";
        const string RESOURCES = "resourceFilePath";

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


        private class ResourcesSchema
        {
            public string basePath;

            public List<string> include, exclude;
        }

        private MetadataSchema metadata;
        private Dictionary<string, object> configs;
        private bool enableTypeCheckAndConversion;

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
            var clonedSelf = this.CloneWithProcessedProperties();
            return JsonConvert.SerializeObject(clonedSelf.configs);
        }

        public ExportConfig CloneWithProcessedProperties()
        {
            var selfClone = this.Clone();
            selfClone.enableTypeCheckAndConversion = false;

            selfClone.Set(CLIENTNAME, "C#");

            if (selfClone.Has(CHARTCONFIG))
            {
                var oldValue = (string)selfClone.Get(CHARTCONFIG);
                selfClone.Remove(CHARTCONFIG);

                selfClone.Set(CHARTCONFIG, ReadFileContent(oldValue, encodeBase64: false));
            }

            if (selfClone.Has(INPUTSVG))
            {
                var oldValue = (string)selfClone.Get(INPUTSVG);
                selfClone.Remove(INPUTSVG);

                selfClone.Set(INPUTSVG, ReadFileContent(oldValue, encodeBase64: true));
            }

            if (selfClone.Has(CALLBACKS))
            {
                var oldValue = (string)selfClone.Get(CALLBACKS);
                selfClone.Remove(CALLBACKS);

                selfClone.Set(CALLBACKS, ReadFileContent(oldValue, encodeBase64: true));
            }

            if (selfClone.Has(DASHBOARDLOGO))
            {
                var oldValue = (string)selfClone.Get(DASHBOARDLOGO);
                selfClone.Remove(DASHBOARDLOGO);

                selfClone.Set(DASHBOARDLOGO, ReadFileContent(oldValue, encodeBase64: true));
            }

            if (selfClone.Has(OUTPUTFILEDEFINITION))
            {
                var oldValue = (string)selfClone.Get(OUTPUTFILEDEFINITION);
                selfClone.Remove(OUTPUTFILEDEFINITION);

                selfClone.Set(OUTPUTFILEDEFINITION, ReadFileContent(oldValue, encodeBase64: false));
            }

            {
                string contentZipbase64, templateFilePathWithinZip;
                selfClone.CreateBase64ZippedTemplate(out contentZipbase64, out templateFilePathWithinZip);

                if (!string.IsNullOrEmpty(contentZipbase64))
                {
                    selfClone.Set(RESOURCES, contentZipbase64);
                }
                if (!string.IsNullOrEmpty(templateFilePathWithinZip))
                {
                    selfClone.Set(TEMPLATE, templateFilePathWithinZip);
                }

            }

            return selfClone;
        }



        private void CreateBase64ZippedTemplate(out string outZipContentBase64, out string outTemplatePathWithinZip)
        {
            var templateFilePath = (string)this.GetValueOrDefault(TEMPLATE, null);
            var resourceFilePath = (string)this.GetValueOrDefault(RESOURCES, null);

            if (!String.IsNullOrEmpty(templateFilePath))
            {
                // Expand templateFilePath
                templateFilePath = Path.GetFullPath(templateFilePath);

                // Load templateFilePath content as html page
                var htmlDoc = new HtmlDocument();
                htmlDoc.Load(templateFilePath);

                // Create map of (filepath within zip folder) : (filepath of original file)
                var listExtractedPaths = new List<string>();

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

                //// Put the modified template file + extracted resources from html + provided resource files in a temp folder
                //var tempZipDirectoryPath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

                //{
                //    // Write modified template file
                //    var htmlContent = htmlDoc.DocumentNode.OuterHtml;
                //    var templateWithinZipFullPath = Path.Combine(tempZipDirectoryPath, "template.html");

                //    File.WriteAllText(templateWithinZipFullPath, htmlContent);
                //}
                //{
                //    // Write extracted resources from html 
                //    foreach (var eachMap in listExtractedPaths)
                //    {
                //        var originalFilePath = eachMap.Key;
                //        var resourceWithinZipFullPath = GetFullPathWrtBasePath(eachMap.Value, tempZipDirectoryPath);

                //        CopyFile(originalFilePath, resourceWithinZipFullPath);
                //    }
                //}

                List<string> listResourcePaths = new List<string>();
                string baseDirectoryPath = null;

                if (!string.IsNullOrEmpty(resourceFilePath))
                {
                    // Resolve resource file full path
                    resourceFilePath = Path.GetFullPath(resourceFilePath);
                    // Get directory path, to be used for glob resolution
                    var resourceDirectoryPath = Path.GetDirectoryName(resourceFilePath);

                    // Load resourceFilePath content (JSON) as instance of Resources
                    var resources = JsonConvert.DeserializeObject<ResourcesSchema>(File.ReadAllText(resourceFilePath));

                    // Resolve include and exclude globs to find the final include list
                    {
                        var listResourceIncludePaths = new List<string>();
                        var listResourceExcludePaths = new List<string>();

                        var root = new DirectoryInfo(resourceDirectoryPath);

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

                        listResourcePaths = listResourceIncludePaths.Except(listResourceExcludePaths).ToList();
                    }

                    baseDirectoryPath = resources.basePath;
                }

                // If basepath is not provided, find it from common ancestor directory of extracted file paths plus template
                if (string.IsNullOrEmpty(baseDirectoryPath))
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
                listResourcePaths = listResourcePaths
                    .Where((tmpPath) => IsWithinPath(tmpPath, baseDirectoryPath))
                    .ToList();

                // Make map relative version of extracted and resource file paths (compared to basepath) with original filepath
                var mapExtractedPathAbsToRel = new Dictionary<string, string>();
                foreach (var tmpPath in listExtractedPaths)
                {
                    mapExtractedPathAbsToRel[tmpPath] = GetRelativePathFrom(tmpPath, baseDirectoryPath);
                }

                var mapResourcePathAbsToRel = new Dictionary<string, string>();
                foreach (var tmpPath in listResourcePaths)
                {
                    mapResourcePathAbsToRel[tmpPath] = GetRelativePathFrom(tmpPath, baseDirectoryPath);
                }

                var templateFilePathWithinZipRel = GetRelativePathFrom(templateFilePath, baseDirectoryPath);

                // Create zip temp folder
                var tempZipDirectoryPath = GetTempFolderName();
                var tempZipFilePath = GetTempFileName();

                // Foreach extracted, resource, template file, create reqd. directory within zip folder and copy them
                foreach (KeyValuePair<string, string> entry in mapExtractedPathAbsToRel)
                {
                    var filePathWithinZipAbs = Path.Combine(tempZipDirectoryPath, entry.Value);
                    var directoryWithinZipAbs = Path.GetDirectoryName(filePathWithinZipAbs);

                    Directory.CreateDirectory(directoryWithinZipAbs);

                    CopyFile(entry.Key, filePathWithinZipAbs);
                }
                foreach (KeyValuePair<string, string> entry in mapResourcePathAbsToRel)
                {
                    var filePathWithinZipAbs = Path.Combine(tempZipDirectoryPath, entry.Value);
                    var directoryWithinZipAbs = Path.GetDirectoryName(filePathWithinZipAbs);

                    Directory.CreateDirectory(directoryWithinZipAbs);
                    CopyFile(entry.Key, filePathWithinZipAbs);
                }

                {
                    var filePathWithinZipAbs = Path.Combine(tempZipDirectoryPath, templateFilePathWithinZipRel);
                    var directoryWithinZipAbs = Path.GetDirectoryName(filePathWithinZipAbs);

                    Directory.CreateDirectory(directoryWithinZipAbs);
                    CopyFile(templateFilePath, filePathWithinZipAbs);
                }

                CreateZipFromDirectory(tempZipDirectoryPath, tempZipFilePath);

                // Set this zip content to _templateZipBase64Content
                outZipContentBase64 = ReadFileContent(tempZipFilePath, encodeBase64: true);
                outTemplatePathWithinZip = templateFilePathWithinZipRel;

                // Delete temp zip folder and temp zip file
                Directory.Delete(tempZipDirectoryPath, true);
                File.Delete(tempZipFilePath);
            }
            else
            {
                outZipContentBase64 = null;
                outTemplatePathWithinZip = null;
            }
        }


    }
}
