using System;
using System.IO;
using FusionCharts.FusionExport.Client;
using System.Collections.Generic;

namespace FusionExportExecutable
{

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                List<string> results = new List<string>();
                string chartConfigFile = "./static/test/chart-config-file2.json";
                // string svgFile = "./static/sample.svg";
                // string resourcesFile = "./static/resources.json";
                string templateFile = "./static/test/dashboard-template.html";

                ExportConfig exportConfig = new ExportConfig();

                using (ExportManager em = new ExportManager("localhost", 1337, false, true))
                {
                    exportConfig.Set("chartConfig", chartConfigFile);
                    exportConfig.Set("templateFilePath", templateFile);
                    results.AddRange(em.Export(exportConfig));
                }

                foreach (string path in results)
                {
                    Console.WriteLine(path);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.Read();
        }
    }
}
