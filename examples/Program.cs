using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FusionExportTest;

namespace examples
{
    class Program
    {
        static void Main(string[] args)
        {
            BulkExport.Run(host: "192.168.0.192",port: 1337);
            Console.ReadKey();
        }
    }
}
