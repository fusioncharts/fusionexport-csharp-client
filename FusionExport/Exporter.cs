using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Threading;

namespace FusionCharts.FusionExport.Client
{
    public class Exporter
    {
        private static int nextExportId = 0;

        private ExportDoneListener exportDoneListener;
        private ExportStateChangedListener exportStateChangedListener;
        private ExportConfig exportConfig;
        private string exportServerHost = Constants.DEFAULT_HOST;
        private int exportServerPort = Constants.DEFAULT_PORT;
        private TcpClient tcpClient;
        private Thread socketConnectionThread;
        private int id = 0;

        public Exporter(ExportConfig exportConfig)
        {
            this.exportConfig = exportConfig;
            this.id = Exporter.nextExportId++;
        }

        public Exporter(ExportConfig exportConfig, ExportDoneListener exportDoneListener)
        {
            this.exportConfig = exportConfig;
            this.exportDoneListener = exportDoneListener;
            this.id = Exporter.nextExportId++;
        }

        public Exporter(ExportConfig exportConfig, ExportStateChangedListener exportStateChangedListener)
        {
            this.exportConfig = exportConfig;
            this.exportStateChangedListener = exportStateChangedListener;
            this.id = Exporter.nextExportId++;
        }

        public Exporter(ExportConfig exportConfig, ExportDoneListener exportDoneListener, ExportStateChangedListener exportStateChangedListener)
        {
            this.exportConfig = exportConfig;
            this.exportDoneListener = exportDoneListener;
            this.exportStateChangedListener = exportStateChangedListener;
            this.id = Exporter.nextExportId++;
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

        public ExportDoneListener ExportDoneListener
        {
            get { return exportDoneListener; }
        }

        public ExportStateChangedListener ExportStateChanged
        {
            get { return exportStateChangedListener; }
        }

        public string ExportServerHost
        {
            get { return exportServerHost; }
        }

        public int ExportServerPort
        {
            get { return exportServerPort; }
        }

        public int Id
        {
            get { return this.id; }
        }

        public void Start()
        {
            this.socketConnectionThread = new Thread(new ThreadStart(HandleSocketConnection));
            this.socketConnectionThread.Start();
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
                this.OnExportDone(null, new ExportException("Exporting has been cancelled"));
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
            }
            catch (Exception ex)
            {
                this.OnExportDone(null, new ExportException(ex.Message));
            }
            finally
            {
                if(this.tcpClient != null)
                    this.tcpClient.Close();

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
                    this.ProcessExportStateChangedData(part);
                } else if(part.StartsWith(Constants.EXPORT_DATA))
                {
                    this.ProcessExportDoneData(part);
                }
            }
            return parts[parts.Length - 1];
        }

        private void ProcessExportStateChangedData(string data)
        {
            string state = data.Remove(0, Constants.EXPORT_EVENT.Length);
            this.OnExportSateChanged(state);
        }

        private void ProcessExportDoneData(string data)
        {
            string exportResult = data.Remove(0, Constants.EXPORT_DATA.Length);
            string exportError = this.CheckExportError(exportResult);
            if (exportError == null)
                this.OnExportDone(exportResult, null);
            else
                this.OnExportDone(null, new ExportException(exportError));
        }

        private string CheckExportError(string exportResult)
        {
            string trimmedExportResult = exportResult.Trim(new char[] { ' ', '\n', '\r', '{', '}' });
            string errorPattern = "^\"error\"\\s*:\\s*\"(.+)\"$";
            if (!Regex.IsMatch(trimmedExportResult, errorPattern, RegexOptions.Singleline))
                return null;
            Match match = Regex.Match(trimmedExportResult, errorPattern, RegexOptions.Singleline);
            return match.Groups[1].Value;
        }

        private void OnExportSateChanged(string state)
        {
            this.exportStateChangedListener?.Invoke(this, state);
        }

        private void OnExportDone(string result, ExportException error)
        {
            this.exportDoneListener?.Invoke(this, result, error);
        }

        private string GetFormattedExportConfigs()
        {
            return String.Format("{0}.{1}<=:=>{2}", "ExportManager", "export", this.exportConfig.GetFormattedConfigs());
        }


    }
}
