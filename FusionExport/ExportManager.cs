using System.Net;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace FusionCharts.FusionExport.Client
{
    public class ExportManager: IDisposable
    {
        private string host;
        private int port;
        private Exporter exporter = null;

        public ExportManager()
        {
            this.host = Constants.DEFAULT_HOST;
            this.port = Constants.DEFAULT_PORT;
            exporter = new Exporter(this.host, this.port);
        }

        public ExportManager(string host, int port)
        {
            this.host = host;
            this.port = port;
            exporter = new Exporter(this.host, this.port);
        }

        ~ExportManager()
        {
            // The object went out of scope and finalized is called
            // Lets call dispose in to release unmanaged resources 
            // the managed resources will anyways be released when GC 
            // runs the next time.
            Dispose(false);
        }

        public void Dispose()
        {
            // If this function is being called the user wants to release the
            // resources. lets call the Dispose which will do this for us.
            Dispose(true);

            // Now since we have done the cleanup already there is nothing left
            // for the Finalizer to do. So lets tell the GC not to call it later.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing == true)
            {
                //Lets release all the managed resources
                ReleaseManagedResources();
            }
        }

        private void ReleaseManagedResources()
        {
            if (exporter != null)
            {
                exporter.Close();
                exporter = null;

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        
        public string Host
        {
            get { return host; }
        }

        public int Port
        {
            get { return port; }
        }

        public List<string> Export(ExportConfig exportConfig)
        {
            return ExportChart(exportConfig);
        }

        public List<string> Export(ExportConfig exportConfig, string outputDir)
        {
            return ExportChart(exportConfig, outputDir, true);
        }

        public List<string> Export(ExportConfig exportConfig, bool unzip)
        {
            return ExportChart(exportConfig, null, unzip);
        }

        public List<string> Export(ExportConfig exportConfig, string outputDir, bool unzip)
        {
            return ExportChart(exportConfig, outputDir, unzip);
        }

        private List<string> ExportChart(ExportConfig exportConfig, string outputDir = "", bool unzip = true)
        {
            exporter.ExportConfig = exportConfig;
            string zipPath = exporter.Start();

            if (string.IsNullOrEmpty(outputDir))
            {
                outputDir = Utils.Utils.GetBaseDirectory();
            }

            DirectoryInfo dirInfo = new DirectoryInfo(outputDir);
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }

            if (!unzip)
            {
                FileInfo fn = new FileInfo(zipPath);
                string fileFullName = Path.Combine(outputDir, fn.Name);
                fn.CopyTo(fileFullName, true);

                return new List<string>(new string[] { fileFullName });
            }
            else
            {                
                return Utils.Utils.ExtractZipInDirectory(zipPath, outputDir);
            }
        }        
    }
}
