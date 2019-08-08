using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FusionCharts.FusionExport.Client; // Import sdk

namespace FusionExportTest
{
    public static class BulkExport
    {
        public static void Run(string host = Constants.DEFAULT_HOST, int port = Constants.DEFAULT_PORT)
        {
            // Instantiate the ExportConfig class and add the required configurations
            ExportConfig exportConfig = new ExportConfig();
            List<string> results = new List<string>();

            // Instantiate the ExportManager class
            using (ExportManager exportManager = new ExportManager())
            {
                exportConfig.Set("chartConfig", "./resources/bulk.json");
                
                // Call the Export() method with the export config
                results.AddRange(exportManager.Export(exportConfig));
            }

            foreach (string path in results)
            {
                Console.WriteLine(path);
            }

            Console.Read();
        }        
    }
}
