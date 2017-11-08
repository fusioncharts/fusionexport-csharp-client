using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.ComponentModel;
using System.IO;
using FusionCharts.FusionExport.Client;

namespace FusionExportExecutable
{

    class Program
    {
        static void Main(string[] args)
        {
            string file = @"C:\Users\rousa\Desktop\Projects\fc-export-java-client\src\com\fusioncharts\chartConfig.json";
            ExportConfig exportConfig = new ExportConfig();
            exportConfig.Set("chartConfig", File.ReadAllText(file));

            ExportManager em = new ExportManager("127.0.0.1", 1377);
            Exporter exporter = em.Export(exportConfig);

            Console.Read();
        }

        
    }
}
