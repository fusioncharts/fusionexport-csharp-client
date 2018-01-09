using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace FusionCharts.FusionExport.Client
{
    public delegate void ExportDoneListener(string result, ExportException error);
    public delegate void ExportStateChangedListener(string state);

    public class ExportManager
    {
        private string host;
        private int port;

        public ExportManager()
        {
            this.host = Constants.DEFAULT_HOST;
            this.port = Constants.DEFAULT_PORT;
        }

        public ExportManager(string host, int port)
        {
            this.host = host;
            this.port = port;
        }

        public string Host
        {
            get { return host; }
            set { host = value; }
        }

        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        public Exporter Export(ExportConfig exportConfig)
        {
            Exporter exporter = new Exporter(exportConfig);
            exporter.SetExportConnectionConfig(this.host, this.port);
            exporter.Start();
            return exporter;
        }

        public Exporter Export(ExportConfig exportConfig, ExportDoneListener exportDoneListener)
        {
            Exporter exporter = new Exporter(exportConfig, exportDoneListener);
            exporter.SetExportConnectionConfig(this.host, this.port);
            exporter.Start();
            return exporter;
        }

        public Exporter Export(ExportConfig exportConfig, ExportStateChangedListener exportStateChangedListener)
        {
            Exporter exporter = new Exporter(exportConfig, exportStateChangedListener);
            exporter.SetExportConnectionConfig(this.host, this.port);
            exporter.Start();
            return exporter;
        }

        public Exporter Export(ExportConfig exportConfig, ExportDoneListener exportDoneListener, ExportStateChangedListener exportStateChangedListener)
        {
            Exporter exporter = new Exporter(exportConfig, exportDoneListener, exportStateChangedListener);
            exporter.SetExportConnectionConfig(this.host, this.port);
            exporter.Start();
            return exporter;
        }


        public static void SaveExportedFiles(string dirPath, string exportedOutput)
        {
            var response = JsonConvert.DeserializeObject<ResponseData>(exportedOutput);

            foreach (var responseElement in response.data)
            {
                var fullPath = Path.Combine(dirPath, responseElement.realName);
                var contentBytes = Convert.FromBase64String(responseElement.fileContent);

                File.WriteAllBytes(fullPath, contentBytes);
            }
        }
        
        public static string[] GetExportedFileNames(string exportedOutput)
        {
            var response = JsonConvert.DeserializeObject<ResponseData>(exportedOutput);

            return response.data.Select((ele) => ele.realName).ToArray();
        }
    }
}
