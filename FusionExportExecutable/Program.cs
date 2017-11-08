using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.ComponentModel;
using FusionCharts.FusionExport.Client;

namespace FusionExportExecutable
{

    class Program
    {
        static void Main(string[] args)
        {
            string data = "THExxQUICKxxBROWNxxFOX";

            string[] parts = data.Split(new string[] { "xx" }, StringSplitOptions.None);
            Console.Write(parts.Length);


            Console.Read();
        }

        static void exportDone(object target, ExportDoneEventArgs e)
        {
            
        }
    }
}
