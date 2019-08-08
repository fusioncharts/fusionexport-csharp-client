# FusionExport C# Client

Language SDK for FusionExport which enables exporting of charts & dashboards through C#.

## Prerequisites

.NET Framework version >= 3.5

## Installation

You can install the SDK using the `NuGet` package manager. To install, open the NuGet package manager console and run the following command:

```sh
$ Install-Package FusionExport
```

## Getting Started

Start with a simple chart export. For exporting a single chart just pass the chart configuration as you would have passed it to the FusionCharts constructor.

```csharp
using System;
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

            // Instantiate the ExportManager class
            using (ExportManager exportManager = new ExportManager())
            {
                exportConfig.Set("chartConfig", chartConfig);

                // Call the Export() method with the export config
                results.AddRange(exportManager.Export(exportConfig, ".\\exported-charts", true));
            }

            foreach (string path in results)
            {
                Console.WriteLine(path);
            }

            Console.Read();
    }
}
```

Finally, run your .NET app. The exported chart will be saved in ./exported-charts folder.

## API Reference

You can find the full reference [here](https://www.fusioncharts.com/dev/exporting-charts/using-fusionexport/sdk-api-reference/c-sharp.html)