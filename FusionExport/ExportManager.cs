using System.Net;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace FusionCharts.FusionExport.Client
{
    public class ExportManager : IDisposable
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

        public List<string> ConvertResultToBase64String(List<string> ExportedFiles)
        {
            if (ExportedFiles == null || ExportedFiles.Count == 0) throw new Exception("List of exported files is empty.");
            //List<string> ExportedFiles = ExportChart(exportConfig, Path.GetTempPath(), true);
            List<string> Base64Data = new List<string>();

            foreach (string file in ExportedFiles)
            {
                Base64Data.Add(Utils.Utils.ReadFileContent(file, true));
            }
            return Base64Data;
        }

        public string ExportBulkParameterHandler(string exportBulk)
        {
            if (exportBulk == "true" || exportBulk == "True")
            {
                return "true";
            }
            else if (exportBulk == "false" || exportBulk == "False")
            {
                return "false";
            }
            else if (exportBulk == "1")
            {
                return "true";
            }
            else if (exportBulk == "0")
            {
                return "false";
            }
            else
            {
                return "false";
            }

        }

        public List<string> Export(ExportConfig exportConfig)
        {
            return (List<string>)ExportChart(exportConfig);
        }

        public List<string> Export(ExportConfig exportConfig, string outputDir)
        {
            return (List<string>)ExportChart(exportConfig, outputDir, true);
        }

        public List<string> Export(ExportConfig exportConfig, bool unzip)
        {
            return (List<string>)ExportChart(exportConfig, null, unzip);
        }

        public List<string> Export(ExportConfig exportConfig, string outputDir, bool unzip, string exportBulk = "false")
        {
            exportConfig.Set("exportBulk", exportBulk);
            exportBulk = this.ExportBulkParameterHandler(exportBulk);
            return (List<string>)ExportChart(exportConfig, outputDir, unzip, exportBulk: exportBulk);
        }

        public Dictionary<string, Stream> ExportAsStream(ExportConfig exportConfig)
        {
            MemoryStream ms = (MemoryStream)ExportChart(exportConfig, "", false, true);

            Dictionary<string, Stream> files = new Dictionary<string, Stream>();

            using (Ionic.Zip.ZipFile z = Ionic.Zip.ZipFile.Read(ms))
            {
                foreach (Ionic.Zip.ZipEntry zEntry in z)
                {
                    MemoryStream tempS = new MemoryStream();
                    zEntry.Extract(tempS);
                    tempS.Seek(0, SeekOrigin.Begin);
                    files.Add(zEntry.FileName, tempS);
                }
            }

            return files;
        }

        public MemoryStream ExportAsStream(ExportConfig exportConfig, string outputDir)
        {
            return (MemoryStream)ExportChart(exportConfig, outputDir, true);
        }

        public MemoryStream ExportAsStream(ExportConfig exportConfig, bool unzip)
        {
            return (MemoryStream)ExportChart(exportConfig, null, unzip);
        }

        public MemoryStream ExportAsStream(ExportConfig exportConfig, string outputDir, bool unzip)
        {
            return (MemoryStream)ExportChart(exportConfig, outputDir, unzip);
        }

        private object ExportChart(ExportConfig exportConfig, string outputDir = "", bool unzip = true, bool ExportAsStream = false, string exportBulk = "false")
        {
            exporter.ExportConfig = exportConfig;
            string zipPath = string.Empty;
            MemoryStream ms = null;

            if (!ExportAsStream)
            {
                zipPath = exporter.Start();
            }
            else
            {
                ms = exporter.AsStream();
                return ms;
            }

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
