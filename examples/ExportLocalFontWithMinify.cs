﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FusionCharts.FusionExport.Client; // Import sdk

namespace FusionExportTest
{
    public static class ExportLocalFontWithMinify
    {
        public static void Run()
        {
            try
            {
                List<string> results = new List<string>();
                string chartConfigFile = System.Environment.CurrentDirectory + "\\resources\\chart-config-file2.json";
                string templateFilePath = System.Environment.CurrentDirectory + "\\resources\\dashboard-template.html";

                ExportConfig exportConfig = new ExportConfig();

                // Instantiate the ExportManager class
                using (ExportManager exportManager = new ExportManager(Constants.DEFAULT_HOST, Constants.DEFAULT_PORT, Constants.DEFAULT_ISSECURE, true))
                {
                    exportConfig.Set("chartConfig", chartConfigFile);
                    exportConfig.Set("templateFilePath", templateFilePath);

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
