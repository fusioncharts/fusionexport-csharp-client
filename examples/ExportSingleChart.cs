using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FusionCharts.FusionExport.Client; // Import sdk

namespace FusionExportTest
{
    public static class ExportSingleChart
    {
        public static void Run(string host = Constants.DEFAULT_HOST, int port = Constants.DEFAULT_PORT)
        {
            // Instantiate the ExportConfig class and add the required configurations
            ExportConfig exportConfig = new ExportConfig();
            List<string> results = new List<string>();

            string chartConfig = @"{
                        ""type"": ""column2d"",
                        ""renderAt"": ""chart-container"",
                        ""width"": ""600"",
                        ""height"": ""400"",
                        ""dataFormat"": ""json"",
                        ""dataSource"": {
                            ""chart"": {
                                ""caption"": ""Number of visitors last week"",
                                ""subCaption"": ""Bakersfield Central vs Los Angeles Topanga""
                            },
                            ""data"": [{
                                    ""label"": ""Mon"",
                                    ""value"": ""15123""
                                },{
                                    ""label"": ""Tue"",
                                    ""value"": ""14233""
                                },{
                                    ""label"": ""Wed"",
                                    ""value"": ""25507""
                                }
                            ]
                        }
                    }";

            string chartConfigFile = System.Environment.CurrentDirectory + "\\resources\\dashboard_charts.json";
            string templateFilePath = System.Environment.CurrentDirectory + "\\resources\\template.html";

            // Instantiate the ExportManager class
            using (ExportManager exportManager = new ExportManager())
            {
                exportConfig.Set("chartConfig", chartConfigFile);
                exportConfig.Set("templateFilePath", templateFilePath);
                exportConfig.Set("templateFormat", "letter");
                exportConfig.Set("type", "pdf");
                // Call the Export() method with the export config
                //results.AddRange(exportManager.Export(exportConfig, @"D:\temp\exported-charts", true));
                results.AddRange(exportManager.Export(exportConfig, System.Environment.GetEnvironmentVariable("%TMP%", EnvironmentVariableTarget.User),true));
            }

            foreach (string path in results)
            {
                Console.WriteLine(path);
            }

            Console.Read();

        }

    }
}
