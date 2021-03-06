using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FusionCharts.FusionExport.Client; // Import sdk

namespace FusionExportTest
{
    public static class DashboardLogoHead
    {
        public static void Run(string host = Constants.DEFAULT_HOST, int port = Constants.DEFAULT_PORT)
        {
            // Instantiate the ExportConfig class and add the required configurations
            ExportConfig exportConfig = new ExportConfig();
            List<string> results = new List<string>();

            // Instantiate the ExportManager class
            using (ExportManager exportManager = new ExportManager())
            {
                exportConfig.Set("chartConfig", File.ReadAllText("./resources/dashboard_charts.json"));
                exportConfig.Set("templateFilePath", "./resources/template.html");
                exportConfig.Set("dashboardLogo", "./resources/logo.png");
                exportConfig.Set("dashboardHeading", "Dashboard");
                exportConfig.Set("dashboardSubheading", "Powered by FusionExport");

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
