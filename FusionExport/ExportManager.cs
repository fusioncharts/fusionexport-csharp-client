using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;

namespace FusionCharts.FusionExport.Client
{
    public delegate void ExportDoneCallback(string result, ExportException error);
    public delegate void ExportStateChangeCallback(string state);

    public class ExportManager
    {
        private string host;
        private int port;

        public ExportManager() {
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

        public Exporter Export(ExportConfig exportConfig, ExportDoneCallback exportDoneCallback)
        {
            Exporter exporter = new Exporter(exportConfig, exportDoneCallback);
            exporter.SetExportConnectionConfig(this.host, this.port);
            exporter.Start();
            return exporter;
        }

        public Exporter Export(ExportConfig exportConfig, ExportStateChangeCallback exportStateChangeCallback)
        {
            Exporter exporter = new Exporter(exportConfig, exportStateChangeCallback);
            exporter.SetExportConnectionConfig(this.host, this.port);
            exporter.Start();
            return exporter;
        }

        public Exporter Export(ExportConfig exportConfig, ExportDoneCallback exportDoneCallback, ExportStateChangeCallback exportStateChangeCallback)
        {
            Exporter exporter = new Exporter(exportConfig, exportDoneCallback, exportStateChangeCallback);
            exporter.SetExportConnectionConfig(this.host, this.port);
            exporter.Start();
            return exporter;
        }

    }
}
