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
            string svgFile = "./static/sample.svg";
            string resourcesFile = "./static/resources.json";
            string templateFile = "./static/html/template.html";

            ExportConfig exportConfig = new ExportConfig();
            exportConfig.Set("chartConfig", chartConfigFile);
            //exportConfig.Set("inputSVG", svgFile);
            //exportConfig.Set("template", templateFile);
            //exportConfig.Set("resources", resourcesFile);

            File.WriteAllText("./a.json",exportConfig.GetFormattedConfigs());

            ExportManager em = new ExportManager();
            em.Export(exportConfig, OnExportDone, OnExportStateChanged);

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
