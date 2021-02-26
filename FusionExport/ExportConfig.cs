using FusionCharts.FusionExport.Utils;
using GlobExpressions;
using HtmlAgilityPack;
using Ionic.Zip;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using static FusionCharts.FusionExport.Utils.Utils;
using System.Text.RegularExpressions;
using NUglify;


namespace FusionCharts.FusionExport.Client
{
    public class ExportConfig : IDisposable
    {
        const string CHARTCONFIG = "chartConfig";
        const string INPUTSVG = "inputSVG";
        const string CALLBACKS = "callbackFilePath";
        const string DASHBOARDLOGO = "dashboardLogo";
        const string OUTPUTFILEDEFINITION = "outputFileDefinition";
        const string CLIENTNAME = "clientName";
        const string TEMPLATE = "templateFilePath";
        const string RESOURCES = "resourceFilePath";
        const string PLATFORM = "platform";
        const string ASYNCCAPTURE = "asyncCapture";
        const string PAYLOAD = "payload";
        const string TEMPLATEURL = "templateURL";
        Boolean minifyResources = Constants.DEFAULT_MINIFY_RESOURCES;
        const string EXPORTBULK = "exportBulk";
        public class MetadataElementSchema
        {
            public enum ElementType
            {
                String,
                boolean,
                Integer,
                Enum
            };
            /*
            public enum SupportedTypes
            {
                String,
                Object,
                File,
                Integer,
                Enum,
                Boolean
            };
            */
            public enum Converter
            {
                PassThrough,
                BooleanConverter,
                NumberConverter,
                ChartConfigConverter,
                EnumConverter,
                FileConverter
            };
            /*
            public enum Dataset
            {
                Letter,
                Legal,
                Tabloid,
                Ledger,
                A0,
                A1,
                A2,
                A3,
                A4,
                A5,
                jpeg,
                jpg,
                png,
                pdf,
                svg,
                html,
                csv,
                xls,
                xlsx,
                good,
                better,
                best
            };
            */
            public ElementType type;
            public Converter converter;
            public string[] supportedTypes; //SupportedTypes 
            public string[] dataset;
            //public Dataset dataset;
        }
        public class MetadataSchema : Dictionary<string, MetadataElementSchema>
        {
            private string[] templateFormat = new string[] { "letter", "legal", "tabloid", "ledger", "a0", "a1", "a2", "a3", "a4", "a5" };

            public static MetadataSchema CreateFromMetaDataJSON()
            {
                /*
                // The below codes are not working, hence came up with another approach, which is to directly accessing the Resource property
                var assembly = typeof(FusionExport.Client.Exporter).Assembly;
                byte[] bytes = null;
                using (Stream resource = assembly.GetManifestResourceStream("FusionExport.Resources.fusionexport-typings.json"))
                {
                    bytes = new byte[resource.Length];
                    resource.Read(bytes, 0, (int)resource.Length);
                }
                var jsonContent = System.Text.Encoding.UTF8.GetString(bytes);
                */

                var jsonContent = System.Text.Encoding.UTF8.GetString(Properties.Resource.fusionexport_typings);
                return JsonConvert.DeserializeObject<MetadataSchema>(jsonContent);
            }

            public Dictionary<MetadataElementSchema.ElementType, Type> TypeMap = new Dictionary<MetadataElementSchema.ElementType, Type>()
            {
                {MetadataElementSchema.ElementType.String, typeof(string)},
                {MetadataElementSchema.ElementType.boolean, typeof(bool)},
                {MetadataElementSchema.ElementType.Integer, typeof(int)},
            };

            public Dictionary<MetadataElementSchema.Converter, Func<object, object, object, object>> ConverterMap = new Dictionary<MetadataElementSchema.Converter, Func<object, object, object, object>>()
            {
                {MetadataElementSchema.Converter.PassThrough, (object configValue,object configName, object metadata) => configValue},
                {MetadataElementSchema.Converter.BooleanConverter, (object configValue, object configName, object metadata) => BooleanFromStringNumber(configValue,configName, metadata)},
                {MetadataElementSchema.Converter.NumberConverter, (object configValue, object configName, object metadata) => NumberFromString(configValue,configName, metadata)},
                {MetadataElementSchema.Converter.ChartConfigConverter, (object configValue, object configName, object metadata) => ChartConfigConverter(configValue,configName, metadata)},
                {MetadataElementSchema.Converter.EnumConverter, (object configValue, object configName, object metadata) => EnumConverter(configValue,configName, metadata)},
                {MetadataElementSchema.Converter.FileConverter, (object configValue, object configName, object metadata) => FileConverter(configValue,configName, metadata)},
            };

            private static object FileConverter(object configValue, object configName, object metadata)
            {
                if (!File.Exists(configValue.ToString()))
                {
                    throw new FileNotFoundException(string.Format("Parameter name: {0} ---> [URL/Path] not found. Please provide an appropriate path.", configName));
                }

                return configValue;
            }

            private static object EnumConverter(object configValue, object configName, object metadata)
            {
                if (configValue.GetType() != typeof(string))
                {
                    string errMsg = string.Format("Invalid Data Type in parameter '{0}'\nData should be a string.", configName.ToString());
                    throw new Exception(errMsg);
                }

                MetadataElementSchema metadataElement = (MetadataElementSchema)metadata;

                if (!metadataElement.dataset.Any(i => i.ToLower().Equals(configValue.ToString().ToLower())))
                {
                    string supportParams = string.Join(", ", metadataElement.dataset);
                    string errMsg = string.Format("Invalid argument value in parameter '{0}'\nSupported parameters are: {1}", configName.ToString(), supportParams);
                    throw new Exception(errMsg);
                }

                return configValue;
            }

            public static bool BooleanFromStringNumber(object configValue, object configName, object metadata)
            {
                if (configValue.GetType() == typeof(bool))
                {
                    return (bool)configValue;
                }
                else if (configValue.GetType() == typeof(string))
                {
                    string value = configValue.ToString().ToLower();

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

            public static int NumberFromString(object configValue, object configName, object metadata)
            {
                if (configValue.GetType() == typeof(int))
                {
                    return (int)configValue;
                }
                else if (configValue.GetType() == typeof(string))
                {
                    string value = configValue.ToString();

                    return Int32.Parse(value);
                }
                else
                {
                    throw new InvalidCastException();
                }
            }

            public static string ChartConfigConverter(object configValue, object configName, object metadata)
            {
                if (configValue.GetType() != typeof(string))
                {
                    string errMsg = string.Format("'{0}' of type '{1}' is unsupported. Supported data types are string.", configName.ToString(), configValue.GetType().Name);
                    throw new Exception(errMsg);
                }

                string valueStr = configValue.ToString();

                if (valueStr.ToLower().EndsWith(".json"))
                {
                    if (!File.Exists(valueStr))
                    {

                        throw new FileNotFoundException(string.Format("{0}\n{1}", valueStr, "Parameter name: chartConfig ---> chartConfig [URL] not found. Please provide an appropriate path."));
                    }

                    // Read the file content and convert to string
                    valueStr = File.ReadAllText(valueStr);
                }
                if (!IsValidJson(valueStr))
                {
                    throw new InvalidDataException("Invalid ChartConfig JSON", new Exception("JSON structure is invalid. Please check your JSON data."));
                }

                return valueStr;
            }

            public void CheckType(Dictionary<string, object> configs, string configName, object configValue)
            {
                string errMsg = string.Empty;

                if (this.ContainsKey(configName))
                {
                    var metadataElement = this[configName];

                    var expectedType = TypeMap[metadataElement.type];

                    if (configValue.GetType() != expectedType)
                    {
                        switch (configName.ToLower())
                        {
                            case "templatefilepath":
                                errMsg = "Data should be in file path of template file";
                                break;
                            case "template":
                                errMsg = "Data should be a HTML template string";
                                break;
                            case "templateurl":
                                errMsg = "Data should be a string";
                                break;
                            case "templatewidth":
                            case "templateheight":
                                errMsg = "Data should be a string or number";
                                break;
                            case "templateformat":
                                errMsg = "Please follow the documentation to learn more.";
                                break;
                        }

                        errMsg = string.Format("Invalid Data Type in parameter '{0}'\n{1}", configName, errMsg);
                        throw new Exception(errMsg);
                    }

                    string valueStr = configValue.ToString().ToLower();

                    switch (configName.ToLower())
                    {
                        case "template":
                            if (!valueStr.StartsWith("<") || !valueStr.EndsWith("</html>"))
                            {
                                errMsg = string.Format("Invalid HTML in parameter '{0}'\nData should be a valid HTML template string.", configName);
                                throw new Exception(errMsg);
                            }
                            break;
                        case "templatefilepath":
                            if (!File.Exists(valueStr))
                            {
                                throw new FileNotFoundException(string.Format("{0}\n{1}", valueStr, "Parameter name: templateFilePath ---> The HTML file which you have provided does not exist. Please provide a valid file."));
                            }
                            break;
                        case "templateurl":
                            try
                            {
                                new Uri(valueStr);
                            }
                            catch
                            {
                                errMsg = string.Format("Invalid URL in parameter '{0}'\nData should be a valid URL", configName);
                                throw new Exception(errMsg);
                            }
                            break;
                        case "templatewidth":
                        case "templateheight":
                            if (configValue.GetType() == typeof(string))
                            {
                                try
                                {
                                    int.Parse(valueStr);
                                }
                                catch
                                {
                                    errMsg = string.Format("Parse Failure in parameter '{0}'\nData should be a parsable number", configName);
                                    throw new Exception(errMsg);
                                }
                            }
                            break;
                        case "templateformat":
                            if (!templateFormat.Contains(valueStr))
                            {
                                errMsg = string.Format("Invalid Format in parameter '{0}'\nInvalid format provided. Please follow the documentation to learn more", configName);
                                throw new Exception(errMsg);
                            }

                            break;
                    }

                    if (configName.Equals("template"))
                    {
                        if (configs.ContainsKey("templateFilePath"))
                        {
                            Console.WriteLine("Both 'templateFilePath' and 'template' is provided. 'templateFilePath' will be ignored.");
                        }
                    }
                    else if (configName.Equals("templateFilePath"))
                    {
                        if (configs.ContainsKey("template"))
                        {
                            Console.WriteLine("Both 'templateFilePath' and 'template' is provided. 'templateFilePath' will be ignored.");
                        }
                    }

                    configs[configName] = configValue;
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
                        convertedValue = ConverterMap[metadataElement.converter](configValue, configName, metadataElement);
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

        private static bool IsValidJson(string jsonString)
        {
            jsonString = jsonString.Trim();
            if ((jsonString.StartsWith("{") && jsonString.EndsWith("}")) || //For object
                (jsonString.StartsWith("[") && jsonString.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = Newtonsoft.Json.Linq.JToken.Parse(jsonString);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return false;
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

        //public ExportConfig(bool enableTypeCheckAndConversion = true)
        public ExportConfig()
        {
            this.enableTypeCheckAndConversion = true;

            this.metadata = MetadataSchema.CreateFromMetaDataJSON();
            this.configs = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        }

        public void Set(string configName, object configValue)
        {
            if (enableTypeCheckAndConversion)
            {
                configValue = this.metadata.TryConvertType(configName, configValue);
                this.metadata.CheckType(configs, configName, configValue);
            }
            else
            {
                configs[configName] = configValue;
            }
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

        public MultipartFormDataContent GetFormattedConfigs(Boolean exportServerMinifyResources)
        {
            this.minifyResources = exportServerMinifyResources;
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

            //var this = this.Clone();
            //this.enableTypeCheckAndConversion = false;

            this.configs[CLIENTNAME] = "C#";
            this.configs[PLATFORM] = Environment.OSVersion.Platform.ToString();

            /*
            if (this.Has(CHARTCONFIG))
            {
                oldValue = this.Get(CHARTCONFIG).ToString();
                string trimmedValue = oldValue.Replace("\n", "").Replace("\t", "").Replace("\r", "");
                this.Remove(CHARTCONFIG);

                if (oldValue.ToLower().EndsWith(".json"))
                {
                    oldValue = ReadFileContent(oldValue, encodeBase64: false);
                }
                else if (((trimmedValue.StartsWith("{") && trimmedValue.EndsWith("}")) || (trimmedValue.StartsWith("[") && trimmedValue.EndsWith("]"))))
                {

                }
                else
                {
                    throw new Exception("Invalid Data Type: Data should be in either serialized JSON, file path of JSON file.");
                }

                this.Set(CHARTCONFIG, oldValue);
            }
            */

            if (this.Has(INPUTSVG))
            {
                oldValue = this.Get(INPUTSVG).ToString();
                this.Remove(INPUTSVG);

                internalFilePath = "inputSVG.svg";
                zipBag.Add(new ResourcePathInfo()
                {
                    internalPath = internalFilePath,
                    externalPath = oldValue
                });
                this.configs[INPUTSVG] = internalFilePath;
                //this.Set(INPUTSVG, internalFilePath);                
            }

            if (this.Has(CALLBACKS))
            {
                oldValue = this.Get(CALLBACKS).ToString();
                this.Remove(CALLBACKS);

                internalFilePath = "callbackFile.js";
                zipBag.Add(new ResourcePathInfo()
                {
                    internalPath = internalFilePath,
                    externalPath = oldValue
                });

                this.configs[CALLBACKS] = internalFilePath;
            }

            if (this.Has(DASHBOARDLOGO))
            {
                oldValue = this.Get(DASHBOARDLOGO).ToString();
                this.Remove(DASHBOARDLOGO);

                var ext = new FileInfo(oldValue).Extension;
                internalFilePath = string.Format("dashboardLogo{0}", ext.StartsWith(".") ? ext : "." + ext);
                zipBag.Add(new ResourcePathInfo()
                {
                    internalPath = internalFilePath,
                    externalPath = oldValue
                });

                this.configs[DASHBOARDLOGO] = internalFilePath;
            }

            if (this.Has(OUTPUTFILEDEFINITION))
            {
                oldValue = this.Get(OUTPUTFILEDEFINITION).ToString();
                this.Remove(OUTPUTFILEDEFINITION);

                this.configs[OUTPUTFILEDEFINITION] = ReadFileContent(oldValue, encodeBase64: false);
            }

            if (this.Has(TEMPLATE))
            {
                string templatePathWithinZip;
                List<ResourcePathInfo> zipPaths;
                this.createTemplateZipPaths(out zipPaths, out templatePathWithinZip);

                this.configs[TEMPLATE] = templatePathWithinZip;

                zipBag.AddRange(zipPaths);
            }

            if (this.Has(ASYNCCAPTURE))
            {
                oldValue = this.Get(ASYNCCAPTURE).ToString();
                this.Remove(ASYNCCAPTURE);

                if (!string.IsNullOrEmpty(oldValue))
                {
                    if (Convert.ToBoolean(oldValue))
                    {
                        this.configs[ASYNCCAPTURE] = true;
                    }
                }
            }

            if (this.Has(EXPORTBULK))
            {
                oldValue = this.Get(EXPORTBULK).ToString();
                this.Remove(ASYNCCAPTURE);

                if (!string.IsNullOrEmpty(oldValue))
                {
                    if (!Convert.ToBoolean(oldValue))
                    {
                        this.configs[EXPORTBULK] = "";
                    }
                }
            }

            if (zipBag.Count > 0)
            {
                string zipFile = ExportConfig.generateZip(zipBag, this.minifyResources);
                this.configs[PAYLOAD] = zipFile;
            }
            zipBag.Clear();

            return this;
        }

        private void createTemplateZipPaths(out List<ResourcePathInfo> outZipPaths, out string outTemplatePathWithinZip)
        {
            string templateFilePath = this.Get(TEMPLATE).ToString();
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            Boolean isMinified = this.minifyResources;
            string minifiedHash = ".min-fusionexport-" + unixTimestamp;
            string minifiedExtension = isMinified ? minifiedHash : "";
            string templatePathWithinZip = "";

            // The template is a HTML body not file
            if (templateFilePath.StartsWith("<"))
            {
                string tempTemplate = GetTempFileName();
                File.WriteAllText(tempTemplate, templateFilePath);
                templateFilePath = tempTemplate;
                this.Set(TEMPLATE, templateFilePath);
            }

            List<string> listExtractedPaths = findResources();
            List<string> listResourcePaths;
            string baseDirectoryPath;
            this.resolveResourceGlobFiles(out baseDirectoryPath, out listResourcePaths);


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

            string[] extensions = new string[] { ".html", ".css", ".js" };
            foreach (var zipPath in zipPaths)
            {
                if (isMinified && extensions.Contains(Path.GetExtension(zipPath.internalPath)))
                {
                    string internalDir = Path.GetDirectoryName(zipPath.internalPath);
                    internalDir = internalDir.Replace(@".\", String.Empty);
                    if (internalDir.Length > 0 && internalDir.Substring(0, 1) == ".")
                    {
                        internalDir = internalDir.Remove(0, 1);
                    }
                    else
                    {
                        internalDir = internalDir + @"\";
                    }
                    string fileExtension = Path.GetExtension(zipPath.internalPath);
                    string fileName = Path.GetFileNameWithoutExtension(zipPath.internalPath);
                    zipPath.internalPath = Path.Combine("template", internalDir + fileName + minifiedExtension + fileExtension);
                }
                else
                {
                    zipPath.internalPath = Path.Combine("template", zipPath.internalPath);
                }
            }

            string extension = Path.GetExtension(templateFilePath);
            Boolean isHtmlJsCss = extensions.Contains(extension);
            if (isMinified && isHtmlJsCss)
            {
                string rawTemplatePath = Path.GetDirectoryName(templateFilePath);
                string templateExtension = Path.GetExtension(templateFilePath);
                string templateFileName = Path.GetFileNameWithoutExtension(templateFilePath);
                string rawTemplateRelativePath = GetRelativePathFrom(rawTemplatePath + @"\" + templateFileName + minifiedExtension + templateExtension, baseDirectoryPath);
                if (rawTemplateRelativePath.Length > 0)
                {
                    rawTemplateRelativePath = rawTemplateRelativePath.Replace(@".\", String.Empty);
                }
                templatePathWithinZip = Path.Combine(
                  "template",
                  rawTemplateRelativePath
                );
            }
            else
            {
                templatePathWithinZip = Path.Combine(
                    "template",
                    GetRelativePathFrom(templateFilePath, baseDirectoryPath)
                );
            }

            outZipPaths = zipPaths;
            outTemplatePathWithinZip = templatePathWithinZip;
            ///CreateBase64ZippedTemplate(out outZipPaths, out outTemplatePathWithinZip);
        }

        private List<string> findResources()
        {
            string templateFilePath = this.Get(TEMPLATE).ToString();

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

                string html = System.IO.File.ReadAllText(templateFilePath);

                MatchCollection htmlFontFaces = Regex.Matches(html, @"@font-face\s*{([\s\S]*?)}", RegexOptions.Multiline);

                foreach (var htmlFontFace in htmlFontFaces)
                {
                    string htmlFontFaceString = htmlFontFace.ToString();
                    MatchCollection htmlFontURLs = Regex.Matches(htmlFontFaceString.ToString(), @"url\((.*?)\)", RegexOptions.Multiline);
                    foreach (var htmlFontURL in htmlFontURLs)
                    {
                        string htmlFontURLString = htmlFontURL.ToString();
                        string htmlFontFilePath = htmlFontURLString.Substring(4, htmlFontURLString.Length - 5);
                        string sanitizedPath = htmlFontFilePath.Replace("\"", string.Empty).Replace("'", string.Empty);
                        if (sanitizedPath != string.Empty)
                        {
                            var resolvedHtmlFontPath = GetFullPathWrtBasePath(sanitizedPath, Path.GetDirectoryName(templateFilePath));
                            listExtractedPaths.Add(resolvedHtmlFontPath);
                        }
                    }
                }

                // Fot these filtered link, script, img tags - map their full resolved filepath to a tempfilename 
                // which will be used within zip and change the URLs (within html) to this tempfilename.
                foreach (var linkTag in filteredLinkTags)
                {
                    var originalFilePath = linkTag.Attributes["href"].Value;
                    var resolvedFilePath = GetFullPathWrtBasePath(originalFilePath, Path.GetDirectoryName(templateFilePath));
                    listExtractedPaths.Add(resolvedFilePath);
                    string css = System.IO.File.ReadAllText(resolvedFilePath);
                    
                    MatchCollection fontFaces = Regex.Matches(css, @"@font-face\s*{([\s\S]*?)}", RegexOptions.Multiline);

                    foreach (var fontFace in fontFaces)
                    {
                        string fontFaceString = fontFace.ToString();
                        MatchCollection fontURLs = Regex.Matches(fontFaceString.ToString(), @"url\((.*?)\)", RegexOptions.Multiline);
                        
                        foreach (var fontURL in fontURLs)
                        {
                            string fontURLString = fontURL.ToString();
                            string fontFilePath = fontURLString.Substring(4, fontURLString.Length - 5);
                            string sanitizedPath = fontFilePath.Replace("\"", string.Empty).Replace("'", string.Empty);
                            if (sanitizedPath != string.Empty)
                            {
                                var resolvedFontPath = GetFullPathWrtBasePath(sanitizedPath, Path.GetDirectoryName(resolvedFilePath));
                                listExtractedPaths.Add(resolvedFontPath);
                            }
                        }
                    }

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

            string resourceFilePath = this.Get(RESOURCES).ToString();
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

            foreach (KeyValuePair<string, string> filepath in listAllFilePaths)
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

        private static string generateZip(List<ResourcePathInfo> fileBag, Boolean isminify)
        {
            var tempZipFilePath = GetTempFileName();
            string[] extensions = new string[] { ".html", ".css", ".js" };
            using (ZipFile zip = new ZipFile())
            {
                foreach (var file in fileBag)
                {
                    string dirPath = file.internalPath.Replace(@"\.\", @"\");
                    string newPath;
                    string fileExtension;
                    fileExtension = Path.GetExtension(file.internalPath);
                    Boolean isHtmlJsCss = extensions.Contains(fileExtension);
                    if (isminify && isHtmlJsCss)
                    {
                        string externalDir = Path.GetDirectoryName(file.externalPath);
                        string fileName = Path.GetFileName(file.internalPath);
                        newPath = externalDir + "/" + fileName;

                        Boolean isHtml = fileExtension == ".html" ? true : false;
                        string htmlFile = "";

                        if (isHtml)
                        {
                            var htmlDoc = new HtmlDocument();
                            htmlDoc.Load(file.externalPath);

                            ExportConfig.updateHtml(out htmlDoc, htmlDoc, "//link", fileBag);
                            ExportConfig.updateHtml(out htmlDoc, htmlDoc, "//script", fileBag);
                            htmlFile = externalDir + "/temp.fusionexport.html";
                            File.WriteAllText(htmlFile, htmlDoc.DocumentNode.WriteTo());

                        }


                        if (Path.GetExtension(file.externalPath).ToLower() == ".css")
                        {
                            var result = Uglify.Css(System.IO.File.ReadAllText(file.externalPath));
                            File.WriteAllText(newPath, result.Code);
                        }
                        if (Path.GetExtension(file.externalPath).ToLower() == ".js")
                        {
                            var result = Uglify.Js(System.IO.File.ReadAllText(file.externalPath));
                            File.WriteAllText(newPath, result.Code);
                        }
                        if (Path.GetExtension(file.externalPath).ToLower() == ".html")
                        {
                            var result = Uglify.Html(System.IO.File.ReadAllText(isHtml ? htmlFile : file.externalPath));
                            File.WriteAllText(newPath, result.Code);
                            if (isHtml) File.Delete(htmlFile);
                        }
                    }
                    else
                    {
                        newPath = file.externalPath;
                    }
                    ZipEntry zipEntry = zip.AddFile(newPath);
                    zipEntry.FileName = dirPath;
                    //if (isminify && isHtmlJsCss) File.Delete(newPath);
                }
                zip.Save(tempZipFilePath);
            }
            return tempZipFilePath;
        }

        private static void updateHtml(out HtmlDocument outDocument, HtmlDocument document, string target, List<ResourcePathInfo> fileBag)
        {
            string property = target == "//script" || target == "//img" ? "src" : "href";
            HtmlNodeCollection tags = document.DocumentNode.SelectNodes(target);
            foreach (var tag in tags)
            {
                if (tag.HasAttributes && tag.Attributes[property] != null && isLocalResource(tag.Attributes[property].Value))
                {
                    var tagValue = tag.Attributes[property].Value.Replace(@"./", String.Empty);
                    tagValue = tagValue.Replace("/", @"\");
                    var found = fileBag.Find((linktag) => linktag.externalPath.Contains(tagValue) && tagValue.Length>0);
                    if (found != null)
                    {
                        tag.SetAttributeValue(property, GetRelativePathFrom(found.internalPath, "template"));
                    }
                }
            }

            outDocument = document;
        }
    }
}
