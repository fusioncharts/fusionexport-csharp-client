using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;

namespace FusionCharts.FusionExport.Client
{
    public delegate void ExportDoneListener(Exporter exporter, string result, ExportException error);
    public delegate void ExportStateChangedListener(Exporter exporter, string state);

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

    }
}
