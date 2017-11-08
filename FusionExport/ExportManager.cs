using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;

namespace FusionCharts.FusionExport.Client
{
    public class ExportManager
    {
        public event ExportDoneEventHandler ExportDone;
        public event ExportStateChangeEventHandler ExportStateChange;

        private string host;
        private int port;
        private ExportConfig exportConfig;
       // private TcpClient tcpClient;

        public ExportManager(string host, int port, ExportConfig exportConfig)
        {
            this.host = host;
            this.port = port;
            this.exportConfig = exportConfig;
        }

        public ExportManager(string host, int port)
        {
            this.host = host;
            this.port = port;
            this.exportConfig = new ExportConfig();
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

        public ExportConfig ExportConfig
        {
            get { return exportConfig; }
            set { exportConfig = value; }
        }

        public void Export(ExportConfig exportConfig)
        {
            Thread th = new Thread(new ThreadStart(this.HandleSocketConnection));
            th.Start();
        }

        public void Export()
        {
            this.Export(this.exportConfig);
        }

        private void HandleSocketConnection()
        {
            try
            {
                // this.tcpClient = new TcpClient(this.host, this.port);
                
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                OnExportDone(new ExportDoneEventArgs(null, null));
            }
        }

        private void OnExportDone(ExportDoneEventArgs e)
        {
            if (this.ExportDone != null)
                this.ExportDone(this, e);
        }

        private void OnExportStateChange(ExportStateChangeEventArgs e)
        {
            if (this.ExportStateChange != null)
                this.ExportStateChange(this, e);
        }
    }
}
