using System;
using System.IO;
using FusionCharts.FusionExport.Client; // Import sdk

namespace FusionExportTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string chartConfigFile = "fullpath/of/dashboard_charts.json";
            string exportServerIP = "127.0.0.1"; // The IP address of export server
            string exportServerPort = 1337; // The Port of export server

            // The export configurations used by export server
            ExportConfig exportConfig = new ExportConfig();
            exportConfig.Set("chartConfig", File.ReadAllText(chartConfigFile));
            exportConfig.Set("templateFilePath", "fullpath/of/template.html");
            exportConfig.Set("callbackFilePath", "fullpath/of/callback.js");

            ExportManager em = new ExportManager(exportServerIP, exportServerPort);
            em.Export(exportConfig, OnExportDone, OnExportStateChanged);
        }
        
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
        
        static void OnExportStateChanged(string state)
        {
            Console.WriteLine("State: " + state);
        }
    }
}