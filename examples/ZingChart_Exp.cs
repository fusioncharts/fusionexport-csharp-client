using System;
using FusionCharts.FusionExport.Client; // Import sdk

namespace FusionExportTest
{
    public static class ZingChart_Exp
    {
        public static void Run(string host = Constants.DEFAULT_HOST, int port = Constants.DEFAULT_PORT)
        {
            // Instantiate the ExportConfig class and add the required configurations
            ExportConfig exportConfig = new ExportConfig();
            exportConfig.Set("templateFilePath", "./resources/template_zingchart.html");
            exportConfig.Set("type", "jpg");
            exportConfig.Set("asyncCapture", true);
            // Instantiate the ExportManager class
            ExportManager em = new ExportManager(host: host, port: port);
            // Call the Export() method with the export config and the respective callbacks
            em.Export(exportConfig, OnExportDone, OnExportStateChanged);
            Console.Read();
        }
        // Called when export is done
        static void OnExportDone(ExportEvent ev, ExportException error)
        {
            if (error != null)
            {
                Console.WriteLine("Error: " + error);
            }
            else
            {
                var fileNames = ExportManager.GetExportedFileNames(ev.exportedFiles);
                Console.WriteLine("Done: " + String.Join(", ", fileNames)); // export result
            }
        }

        // Called on each export state change
        static void OnExportStateChanged(ExportEvent ev)
        {
            Console.WriteLine("State: " + ev.state.customMsg);
        }
    }
}
