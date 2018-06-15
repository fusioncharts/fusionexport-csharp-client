using System;
using System.IO;
using System.Linq;
using FusionCharts.FusionExport.Client; // Import sdk

namespace FusionExportTest
{
    public static class AmazonS3_Upload
    {
        public static void Run(string host = Constants.DEFAULT_HOST, int port = Constants.DEFAULT_PORT)
        {
            // Instantiate the ExportConfig class and add the required configurations
            ExportConfig exportConfig = new ExportConfig();
            exportConfig.Set("chartConfig", File.ReadAllText("./resources/bulk.json"));

            // Instantiate the ExportManager class
            ExportManager em = new ExportManager(host: host, port: port);
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
                string bucket, accessKey, secretAccessKey;
                var fileNames = ExportManager.GetExportedFileNames(ev.exportedFiles);
                Console.WriteLine("Done: " + String.Join(", ", fileNames)); // export result
                Console.WriteLine("Bucket Name: ");
                bucket = Console.ReadLine();
                Console.WriteLine("Access Key ID: ");
                accessKey = Console.ReadLine();
                Console.WriteLine("Secret Access Key ID: ");
                secretAccessKey = Console.ReadLine();
                ExportManager.UploadFileToAmazonS3(ev.exportedFiles, bucket, accessKey, secretAccessKey);
            }
        }

        // Called on each export state change
        static void OnExportStateChanged(ExportEvent ev)
        {
            Console.WriteLine("State: " + ev.state.customMsg);
        }
    }
}
