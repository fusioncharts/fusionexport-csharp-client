using System;
using System.IO;
using System.Linq;
using FusionCharts.FusionExport.Client; // Import sdk

namespace FusionExportTest
{
    public static class AsyncCapture
    {
        public static void Run(string host = Constants.DEFAULT_HOST, int port = Constants.DEFAULT_PORT)
        {
            // Instantiate the ExportConfig class and add the required configurations
            ExportConfig exportConfig = new ExportConfig();
            exportConfig.Set("chartConfig", File.ReadAllText("./resources/scrollchart.json"));
            exportConfig.Set("callbackFilePath", "./resources/expand_scroll.js");
            exportConfig.Set("asyncCapture", "true");

            // Instantiate the ExportManager class
            ExportManager em = new ExportManager(host:host, port:port);
            // Call the Export() method with the export config and the respective callbacks
            em.Export(exportConfig, OnExportDone, OnExportStateChanged);
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
                Console.WriteLine("Done: " + String.Join(", ",fileNames)); // export result
            }
        }

        // Called on each export state change
        static void OnExportStateChanged(ExportEvent ev)
        {
            Console.WriteLine("State: " + ev.state.customMsg);
        }
    }
}