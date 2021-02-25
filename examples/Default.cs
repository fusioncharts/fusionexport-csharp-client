using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

using FusionCharts.FusionExport.Client; // Import sdk

namespace FusionExportTest
{
    public static class Default
    {
        public static void Run(string host = Constants.DEFAULT_HOST, int port = Constants.DEFAULT_PORT)
        {
            // Instantiate the ExportConfig class and add the required configurations
            ExportConfig exportConfig = new ExportConfig();
            List<string> results = new List<string>();

            string chartConfig = @"{
                        ""type"": ""column2d"",
                        ""renderAt"": ""chart-container"",
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
                exportConfig.Set("chartConfig", chartConfig);
                exportConfig.Set("templateFormat", "letter");
                exportConfig.Set("header", "Test Header");
                exportConfig.Set("subheader", "Test Sub Header");

                // Call the Export() method with the export config
                //results.AddRange(exportManager.Export(exportConfig, @"D:\temp\exported-charts", true));
                results.AddRange(exportManager.Export(exportConfig, System.Environment.GetEnvironmentVariable("%TMP%", EnvironmentVariableTarget.User), true, false));
            }

            foreach (string path in results)
            {
                Console.WriteLine(path);
            }

            Console.Read();
        }

    }
}
