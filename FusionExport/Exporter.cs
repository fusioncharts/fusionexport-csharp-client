using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace FusionCharts.FusionExport.Client
{
    public class Exporter : IDisposable
    {
        private ExportConfig exportConfig;
        private string exportServerHost = Constants.DEFAULT_HOST;
        private int exportServerPort = Constants.DEFAULT_PORT;
        private Boolean exportServerIsSecure = Constants.DEFAULT_ISSECURE;
        private Boolean exportServerMinifyResources = Constants.DEFAULT_MINIFY_RESOURCES;
        private String exportServerProtocol = Constants.UNSECURED_PROTOCOL;
        private HttpClient httpClient;
        private string requestUri;

        public Exporter(string exportServerHost, int exportServerPort, Boolean exportServerIsSecure, Boolean exportServerMinifyResources)
        {
            this.exportServerHost = exportServerHost;
            this.exportServerPort = exportServerPort;
            this.exportServerIsSecure = exportServerIsSecure;
            this.exportServerMinifyResources = exportServerMinifyResources;
            if (this.exportServerIsSecure) {
                this.setExportServerProtocol(Constants.SECURED_PROTOCOL);
            }else
            {
                this.setExportServerProtocol(Constants.UNSECURED_PROTOCOL);
            }

            // Create full http url path. This looks like `http://127.0.0.1:2020/a/b`,
            // Currently, we join host and port using `:` (naive solution) to get this string.
            //
            // TODO: Ideally, we should parse hostname and put port number just after ip address or host provided.

            this.requestUri = string.Format("{0}://{1}:{2}/api/v2.0/export",
                this.exportServerProtocol,
                this.exportServerHost,
                this.exportServerPort.ToString());
                      
            // Create a new http client object
            this.httpClient = new HttpClient();
            
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)(0xc0 | 0x300 | 0xc00);
            ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback
            (
            delegate { return true; }
            );

            httpClient.BaseAddress = new Uri(this.requestUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();

            if (this.exportServerIsSecure)
            {
                try
                {
                    using (Task<HttpResponseMessage> task = httpClient.GetAsync(this.requestUri))
                    {
                        task.Wait();
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("HTTPS server not found, overriding requests to an HTTP server");
                    this.setExportServerProtocol(Constants.UNSECURED_PROTOCOL);
                    this.requestUri = string.Format("{0}://{1}:{2}/api/v2.0/export",
                    this.exportServerProtocol,
                    this.exportServerHost,
                    this.exportServerPort.ToString());
                    this.httpClient = new HttpClient();
                    httpClient.BaseAddress = new Uri(this.requestUri);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                }
            }
        }

        public void setExportServerProtocol(String protocol)
        {
            this.exportServerProtocol = protocol;
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
            return HandleHttpConnection().ToString();
        }

        public MemoryStream AsStream()
        {
            return (MemoryStream)HandleHttpConnection(true);
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

        private object HandleHttpConnection()
        {
            return HandleHttpConnection(false);
        }
        private object HandleHttpConnection(bool ExportAsStream = false)
        {
            try
            {
                string tempZipFilePath = string.Empty;
                System.IO.MemoryStream ms = new System.IO.MemoryStream();

                using (MultipartFormDataContent multipartFormData = this.exportConfig.GetFormattedConfigs(this.exportServerMinifyResources))
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
                                            if (ExportAsStream)
                                            {
                                                respMessage.Content.ReadAsStreamAsync().Wait();
                                                respMessage.Content.CopyToAsync(ms).Wait();
                                                ms.Seek(0, SeekOrigin.Begin);
                                            }
                                            else
                                            {
                                                respMessage.Content.ReadAsStreamAsync().Wait();
                                                fileStream.SetLength((long)respMessage.Content.Headers.ContentLength);
                                                respMessage.Content.CopyToAsync(fileStream).Wait();
                                                fileStream.Flush();
                                                fileStream.Close();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Task<string> responseText = respMessage.Content.ReadAsStringAsync();
                                        throw new FusionExportHttpException("Server Error - " + responseText.Result);
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

                if (ExportAsStream)
                {
                    return ms;
                }
                else
                {
                    return tempZipFilePath;
                }
            }
            catch (Exception exception)
            {
                if (exception is AggregateException)
                {
                    foreach (var e in ((AggregateException)exception).Flatten().InnerExceptions)
                    {
                        Console.WriteLine(e.StackTrace);
                        if (e is HttpRequestException)
                        {
                            throw new FusionExportHttpException(string.Format("Connection Refused:\nUnable to connect to FusionExport server. Make sure that your server is running on {0}://{1}:{2}", this.exportServerProtocol, this.ExportServerHost, this.ExportServerPort));
                        }
                        else
                        {
                            throw new FusionExportHttpException(exception.InnerException.Message);
                        }
                    }
                }
                throw new FusionExportHttpException(exception.Message);
            }
        }

    }
}
