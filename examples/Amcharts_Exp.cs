using System;
using FusionCharts.FusionExport.Client; // Import sdk

namespace FusionExportTest
{
    public static class Amcharts_Exp
    {
        public static void Run(string host = Constants.DEFAULT_HOST, int port = Constants.DEFAULT_PORT)
        {
            // Instantiate the ExportConfig class and add the required configurations
            ExportConfig exportConfig = new ExportConfig();
            exportConfig.Set("templateFilePath", "./resources/template_amcharts.html");
            exportConfig.Set("type", "jpg");
            exportConfig.Set("asyncCapture", true);
            // Instantiate the ExportManager class
            ExportManager em = new ExportManager(host: host, port: port);
            // Call the Export() method with the export config and the respective callbacks
            em.Export(exportConfig);
            Console.Read();
        }
    }
}
