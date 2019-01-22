using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Threading;
//using WebSocket4Net;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;

namespace FusionCharts.FusionExport.Client
{
    public class Exporter : IDisposable
    {
        private ExportConfig exportConfig;
        private string exportServerHost = Constants.DEFAULT_HOST;
        private int exportServerPort = Constants.DEFAULT_PORT;
        private HttpClient httpClient;
        private string requestUri;

        public Exporter(string exportServerHost, int exportServerPort)
        {
            this.exportServerHost = exportServerHost;
            this.exportServerPort = exportServerPort;

            // Create full http url path. This looks like `http://127.0.0.1:2020/a/b`,
            // Currently, we join host and port using `:` (naive solution) to get this string.
            //
            // TODO: Ideally, we should parse hostname and put port number just after ip address or host provided.
            this.requestUri = string.Format("{0}:{1}/api/v2.0/export",
                this.exportServerHost,
                this.exportServerPort.ToString());


            // Create a new http client object
            this.httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(this.requestUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();

        }

        public void Dispose()
        {
            this.Close();
            httpClient = null;
            if (exportConfig != null)
            {
                exportConfig.Dispose();
                exportConfig = null;
            }
        }

        public ExportConfig ExportConfig
        {
            get { return exportConfig; }
            set { this.exportConfig = value; }
        }

        public string ExportServerHost
        {
            get { return exportServerHost; }
        }

        public int ExportServerPort
        {
            get { return exportServerPort; }
        }

        public string Start()
        {
            return HandleHttpConnection();
        }

        public void Cancel()
        {
            this.Close();
        }

        public void Close()
        {
            try
            {
                if (this.httpClient != null)
                {
                    this.httpClient.Dispose();
                }
            }
            catch (Exception) { }
        }

        private string HandleHttpConnection()
        {
            try
            {
                string tempZipFilePath = string.Empty;

                using (MultipartFormDataContent multipartFormData = this.exportConfig.GetFormattedConfigs())
                {
                    using (Task<HttpResponseMessage> task = httpClient.PostAsync(this.requestUri, multipartFormData))
                    {
                        task.Wait();

                        task.ContinueWith((antecedent) =>
                        {
                            if (antecedent.Result != null)
                            {
                                using (HttpResponseMessage respMessage = antecedent.Result)
                                {
                                    if (respMessage.StatusCode == HttpStatusCode.OK)
                                    {
                                        string fName = respMessage.Content.Headers.ContentDisposition.FileName.Replace("\"", string.Empty).Trim();
                                        tempZipFilePath = System.IO.Path.Combine(Utils.Utils.GetTempFolderName(true), fName);
                                        using (var fileStream = System.IO.File.Create(tempZipFilePath))
                                        {
                                            respMessage.Content.ReadAsStreamAsync().Wait();
                                            fileStream.SetLength((long)respMessage.Content.Headers.ContentLength);
                                            respMessage.Content.CopyToAsync(fileStream).Wait();
                                            fileStream.Flush();
                                            fileStream.Close();
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception("Server Error - " + respMessage.StatusCode);
                                    }
                                }
                                antecedent.Dispose();
                            }
                            else
                            {
                                if (antecedent.Exception != null)
                                {
                                    throw antecedent.Exception;
                                }
                            }

                        }).Wait();
                    }
                }

                return tempZipFilePath;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.InnerException != null)
                    {
                        throw new FusionExportHttpException(ex.InnerException.InnerException.Message, ex.InnerException.InnerException);
                    }
                    else
                    {
                        throw new FusionExportHttpException(ex.InnerException.Message, ex.InnerException);
                    }
                }
                else
                {
                    throw new FusionExportHttpException(ex.Message, ex);
                }
            }
        }
    }
}
