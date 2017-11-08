using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace FusionCharts.FusionExport.Client
{
    public class Exporter
    {
        private ExportDoneCallback exportDoneCallback;
        private ExportStateChangeCallback exportStateChangeCallback;
        private ExportConfig exportConfig;
        private string exportServerHost = Constants.DEFAULT_HOST;
        private int exportServerPort = Constants.DEFAULT_PORT;
        private TcpClient tcpClient;
        private Thread exportConnectionThread;

        public Exporter(ExportConfig exportConfig)
        {
            this.exportConfig = exportConfig;
        }

        public Exporter(ExportConfig exportConfig, ExportDoneCallback exportDoneCallback)
        {
            this.exportConfig = exportConfig;
            this.exportDoneCallback = exportDoneCallback;
        }

        public Exporter(ExportConfig exportConfig, ExportStateChangeCallback exportStateChangeCallback)
        {
            this.exportConfig = exportConfig;
            this.exportStateChangeCallback = exportStateChangeCallback;
        }

        public Exporter(ExportConfig exportConfig, ExportDoneCallback exportDoneCallback, ExportStateChangeCallback exportStateChangeCallback)
        {
            this.exportConfig = exportConfig;
            this.exportDoneCallback = exportDoneCallback;
            this.exportStateChangeCallback = exportStateChangeCallback;
        }

        public void SetExportConnectionConfig(string exportServerHost, int exportServerPort)
        {
            this.exportServerHost = exportServerHost;
            this.exportServerPort = exportServerPort;
        }

        public ExportConfig ExportConfig
        {
            get { return exportConfig; }
        }

        public ExportDoneCallback ExportDoneCallback
        {
            get { return exportDoneCallback; }
        }

        public ExportStateChangeCallback ExportStateChangeCallback
        {
            get { return exportStateChangeCallback; }
        }

        public string ExportServerHost
        {
            get { return exportServerHost; }
        }

        public int ExportServerPort
        {
            get { return exportServerPort; }
        }

        public void Start()
        {
            this.exportConnectionThread = new Thread(new ThreadStart(HandleSocketConnection));
            this.exportConnectionThread.Start();
        }

        public void Cancel()
        {
            try
            {
                if(this.tcpClient != null)
                {
                    this.tcpClient.Close();
                }
            } catch(Exception) {}
            finally
            {
                if(this.exportDoneCallback != null)
                {
                    this.exportDoneCallback(null, new ExportException("Exporting has been cancelled"));
                }
            }
        }

        private void HandleSocketConnection()
        {
            try
            {
                this.tcpClient = new TcpClient(this.exportServerHost, this.exportServerPort);
                NetworkStream stream = this.tcpClient.GetStream();
                byte[] writeBuffer = Encoding.UTF8.GetBytes(this.GetFormattedExportConfigs());
                stream.Write(writeBuffer, 0, writeBuffer.Length);
                stream.Flush();

                byte[] readBuffer = new byte[this.tcpClient.ReceiveBufferSize];
                string dataReceived = "";
                int read = 0;
                while((read = stream.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    dataReceived += Encoding.UTF8.GetString(readBuffer, 0, read);
                    dataReceived = this.ProcessDataReceived(dataReceived);
                }

                stream.Close();
                this.tcpClient.Close();
            }
            catch (Exception ex)
            {
                // Console.Write(ex);
                if(this.exportDoneCallback != null)
                {
                    this.exportDoneCallback(null, new ExportException(ex.Message));
                }
            }
        }

        private string ProcessDataReceived(string data)
        {
            string[] parts = data.Split(new string[] { Constants.UNIQUE_BORDER }, StringSplitOptions.None);
            for(int i = 0; i<parts.Length - 1; i++)
            {
                string part = parts[i];
                if(part.StartsWith(Constants.EXPORT_EVENT))
                {
                    if(this.exportStateChangeCallback != null)
                    {
                        this.exportStateChangeCallback(part.Remove(0, Constants.EXPORT_EVENT.Length));
                    }
                } else if(part.StartsWith(Constants.EXPORT_DATA))
                {
                    // TODO: handle error message
                    if(this.exportDoneCallback != null)
                    {
                        this.exportDoneCallback(part.Remove(0, Constants.EXPORT_DATA.Length), null);
                    }
                }
            }
            Console.Write(parts[parts.Length - 1]);
            return parts[parts.Length - 1];
        }

        private string GetFormattedExportConfigs()
        {
            return String.Format("{0}.{1}<=:=>{2}", "ExportManager", "export", this.exportConfig.GetFormattedConfigs());
        }


    }
}
