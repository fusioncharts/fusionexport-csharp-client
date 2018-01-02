using System;
using System.IO;
using FusionCharts.FusionExport.Client;

namespace FusionExportExecutable
{

    class Program
    {
        static void Main(string[] args)
        {
            string chartConfigFile = "./static/chart-config.json";

            ExportConfig exportConfig = new ExportConfig();
            exportConfig.Set("chartConfig", File.ReadAllText(chartConfigFile));


            ExportManager em = new ExportManager();
            em.Export(exportConfig, OnExportDone, OnExportStateChanged);
            File.WriteAllText("./a.json",exportConfig.GetFormattedConfigs());

            Console.Read(); 
        }

        static void OnExportDone(string result, ExportException error)
        {

            if(error != null)
            {
                Console.WriteLine("ERROR: " + error);
            } else
            {
                Console.WriteLine("DONE: " + result);
            }
        }

        static void OnExportStateChanged(string state)
        {
            Console.WriteLine("STATE: " + state);
        }
    }
}
