# FusionExport C# Client

Language SDK for FusionExport which enables exporting of charts & dashboards through C#.

## Prerequisites

.NET Framework version >= 3.5

## Installation

You can install the SDK using the `NuGet` package manager. To install, open the NuGet package manager console and
run the following command:

```sh
$ Install-Package FusionExport
```

## Getting Started

After installing the SDK in your .NET project, create a new file named `chart-config.json`, 
which will contain the chart configurations to be exported. Before exporting your chart, make sure the export server is running.

The `chart-config.json` file:

```json
[
  {
    "type": "column2d",
    "renderAt": "chart-container",
    "width": "600",
    "height": "200",
    "dataFormat": "json",
    "dataSource": {
      "chart": {
        "caption": "Number of visitors last week",
        "subCaption": "Bakersfield Central vs Los Angeles Topanga"
      },
      "data": [
        {
          "label": "Mon",
          "value": "15123"
        },
        {
          "label": "Tue",
          "value": "14233"
        },
        {
          "label": "Wed",
          "value": "25507"
        }
      ]
    }
  }
]
```

Now import the SDK library into your project and write the export logic in your project as given below:

```csharp
using System;
using System.IO;
using FusionCharts.FusionExport.Client; // Import sdk

namespace FusionExportTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // Instantiate the ExportConfig class and add the required configurations
            ExportConfig exportConfig = new ExportConfig();
            exportConfig.Set("chartConfig", File.ReadAllText("fullpath/of/chart-config.json"));

            // Instantiate the ExportManager class
            ExportManager em = new ExportManager();
            // Call the Export() method with the export config and the respective callbacks
            em.Export(exportConfig, OnExportDone, OnExportStateChanged);
        }
        
        // Called when export is done
        static void OnExportDone(string result, ExportException error)
        {
            if(error != null)
            {
                Console.WriteLine("Error: " + error);
            } else
            {   
                Console.WriteLine("Done: " + result); // export result
            }
        }
        
        // Called on each export state change
        static void OnExportStateChanged(string state)
        {
            Console.WriteLine("State: " + state);
        }
    }
}

```

Finally, run your .NET app. The exported chart will be received when the `ExportDone` event is triggered .

## API Reference

You can find the full reference [here](https://www.fusioncharts.com/dev/exporting-charts/using-fusionexport/sdk-api-reference/c-sharp.html)