using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Threading;
using WebSocket4Net;
using System.Threading.Tasks;

namespace FusionCharts.FusionExport.Client
{
    public class Exporter
    {
        private ExportDoneListener exportDoneListener;
        private ExportStateChangedListener exportStateChangedListener;
        private ExportConfig exportConfig;
        private string exportServerHost = Constants.DEFAULT_HOST;
        private int exportServerPort = Constants.DEFAULT_PORT;
        private WebSocket wsClient;
        private Thread wsConnectionThread;
        private TaskCompletionSource<string> taskCompletion;

        public Exporter(ExportConfig exportConfig)
        {
            this.exportConfig = exportConfig;
        }

        public Exporter(ExportConfig exportConfig, ExportDoneListener exportDoneListener)
        {
            this.exportConfig = exportConfig;
            this.exportDoneListener = exportDoneListener;
        }

        public Exporter(ExportConfig exportConfig, ExportStateChangedListener exportStateChangedListener)
        {
            this.exportConfig = exportConfig;
            this.exportStateChangedListener = exportStateChangedListener;
        }

        public Exporter(ExportConfig exportConfig, ExportDoneListener exportDoneListener, ExportStateChangedListener exportStateChangedListener)
        {
            this.exportConfig = exportConfig;
            this.exportDoneListener = exportDoneListener;
            this.exportStateChangedListener = exportStateChangedListener;
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

        public ExportDoneListener ExportDone
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

        public void Start()
        {
            this.wsConnectionThread = new Thread(new ThreadStart(HandleWSConnection));
            this.wsConnectionThread.Start();
        }

        public void Cancel()
        {
            this.Close("Connection forcibly cancelled");
        }
        public void Close(string reasonText = "Connection closed")
        {
            try
            {
                if (this.wsClient != null)
                {
                    this.wsClient.Close(reason:reasonText);
                }
            }
            catch (Exception) { }
        }

        private void HandleWSConnection()
        {

            // Create full websocket path. This looks like `1.1.1.1:2020\a\b` (scheme://host:port/path?query),
            // Currently, we join host and port using `:` (naive solution) to get this string.
            //
            // TODO: Ideally, we should parse hostname and put port number just after ip adress or host provided.
            // This will take care of trailing `slash` and `path` in the url.
            var fullWSPath = "ws://" + string.Join(":", new string[]{
                    this.ExportServerHost,
                    this.ExportServerPort.ToString()
                });

            // Create TaskCompletionSource
            this.taskCompletion = new TaskCompletionSource<string>();

            // Create new WebSocket with this path
            this.wsClient = new WebSocket(fullWSPath);

            // Set incoming data handler before connecting
            this.wsClient.MessageReceived += (sender, e) =>
            {
                string dataReceived = e.Message;
                dataReceived = this.ProcessDataReceived(dataReceived);
            };

            // Set error handler
            this.wsClient.Error += (sender, e) =>
            {
                this.OnExportDone(null, new ExportException(e.Exception.Message));
                taskCompletion.TrySetException(e.Exception);
            };

            // Set sending data onopened
            this.wsClient.Opened += (sender, e) =>
            {
                // Send data as buffer
                string writeString = this.GetFormattedExportConfigs();
                wsClient.Send(writeString);
            };

            // Connect to the websocket. Incoming data and error handlers should already be set.
            this.wsClient.Open();

            this.taskCompletion.Task.Wait();
        }

        private string ProcessDataReceived(string data)
        {
            string[] parts = data.Split(new string[] { Constants.UNIQUE_BORDER }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.StartsWith(Constants.EXPORT_EVENT))
                {
                    this.ProcessExportStateChangedData(part);
                }
                else if (part.StartsWith(Constants.EXPORT_DATA))
                {
                    this.ProcessExportDoneData(part);
                }
            }
            return parts[parts.Length - 1];
        }

        private void ProcessExportStateChangedData(string data)
        {
            string state = data.Remove(0, Constants.EXPORT_EVENT.Length);
            string exportError = this.CheckExportError(state);
            if (exportError == null)
                this.OnExportSateChanged(state);
            else
                this.OnExportDone(null, new ExportException(exportError));
        }

        private void ProcessExportDoneData(string data)
        {
            string exportResult = data.Remove(0, Constants.EXPORT_DATA.Length);
            this.OnExportDone(exportResult, null);
            this.taskCompletion.TrySetResult(exportResult);
        }

        private string CheckExportError(string state)
        {
            string trimmedExportResult = state.Trim(new char[] { ' ', '\n', '\r', '{', '}' });
            string errorPattern = "^\"error\"\\s*:\\s*\"(.+)\"$";
            if (!Regex.IsMatch(trimmedExportResult, errorPattern, RegexOptions.Singleline))
                return null;
            Match match = Regex.Match(trimmedExportResult, errorPattern, RegexOptions.Singleline);
            return match.Groups[1].Value;
        }

        private void OnExportSateChanged(string state)
        {
            var exportEvent = new ExportEvent()
            {
                state = StateChangeData.FromStateString(state)
            };

            this.exportStateChangedListener?.Invoke(exportEvent);
        }

        private void OnExportDone(string result, ExportException error)
        {
            if (error == null)
            {
                var exportEvent = new ExportEvent()
                {
                    exportedFiles = ExportCompleteData.FromResponseString(result)
                };

                this.exportDoneListener?.Invoke(exportEvent, error);
            }
            else
            {
                this.exportDoneListener?.Invoke(null, error);
            }
            
            this.Close();
        }

        private string GetFormattedExportConfigs()
        {
            return String.Format("{0}.{1}<=:=>{2}", "ExportManager", "export", this.exportConfig.GetFormattedConfigs());
        }


    }
}
