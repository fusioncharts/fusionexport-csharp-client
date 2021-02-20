using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FusionCharts.FusionExport.Client; // Import sdk

namespace FusionExportTest
{
    public static class ExportSingleViaHttps
    {
        public static void Run(string host = Constants.DEFAULT_HOST, int port = Constants.DEFAULT_PORT, Boolean isSecure = Constants.DEFAULT_ISSECURE)
        {
            try
            {
                List<string> results = new List<string>();
                string chartConfigFile = System.Environment.CurrentDirectory + "\\resources\\single.json";

                ExportConfig exportConfig = new ExportConfig();

                // Instantiate the ExportManager class
                using (ExportManager exportManager = new ExportManager(host, port, isSecure))
                {
                    exportConfig.Set("chartConfig", chartConfigFile);

                    // Call the Export() method with the export config
                    //results.AddRange(exportManager.Export(exportConfig, @"D:\temp\exported-charts", true));
                    results.AddRange(exportManager.Export(exportConfig, System.Environment.GetEnvironmentVariable("%TMP%", EnvironmentVariableTarget.User), true));
                }

                foreach (string path in results)
                {
                    Console.WriteLine(path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.Read();
        }

    }
}
