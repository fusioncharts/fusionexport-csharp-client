using Amazon.S3;
using Amazon.S3.Model;
using System.Net;
using System;
using System.IO;
using System.Linq;


namespace FusionCharts.FusionExport.Client
{
    public delegate void ExportDoneListener(ExportEvent exportEvent, ExportException error);
    public delegate void ExportStateChangedListener(ExportEvent exportEvent);

    public class ExportManager
    {
        private string host;
        private int port;

        public ExportManager()
        {
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


        public static void SaveExportedFiles(string dirPath, ExportCompleteData exportedFiles)
        {
           
            foreach (var fileElement in exportedFiles.data)
            {
                var fullPath = Path.Combine(dirPath, fileElement.realName);
                var contentBytes = Convert.FromBase64String(fileElement.fileContent);

                File.WriteAllBytes(fullPath, contentBytes);
                
            }
        }
        /// <summary>
        /// return region of that particular bucket
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="accessKey"></param>
        /// <param name="secretAccessKey"></param>
        /// <returns></returns>
        private static S3Region GetBucketRegion(string bucketName,string accessKey, string secretAccessKey)
        {
            // create client with a default region. in our case it is "USEAST1"
            AmazonS3Client client = new AmazonS3Client(accessKey, secretAccessKey, Amazon.RegionEndpoint.USEast1);
            GetBucketLocationRequest request = new GetBucketLocationRequest();
            request.BucketName = bucketName;
            GetBucketLocationResponse response = client.GetBucketLocation(request);
            return response.Location;


        }
        /// <summary>
        /// upload exported file(s) to AS3
        /// </summary>
        /// <param name="exportedFiles"></param>
        /// <param name="bucket"></param>
        /// <param name="accessKey"></param>
        /// <param name="secretAccessKey"></param>
        public static void UploadFileToAmazonS3(ExportCompleteData exportedFiles, string bucket, string accessKey, string secretAccessKey)
        {
            // get region of the provided bucket in which user wants to upload the file
            S3Region region;
            try
            {
                region = GetBucketRegion(bucket, accessKey, secretAccessKey);
            }
            catch(AmazonS3Exception)
            {
                throw new FusionExportAmazonS3Exception("Incorrect credentials");
            }
           

            AmazonS3Client client = new AmazonS3Client(accessKey, secretAccessKey, Amazon.RegionEndpoint.GetBySystemName(region.Value.ToString()));

            PutObjectRequest request = new PutObjectRequest();

            foreach(var fileElement in exportedFiles.data)
            {
                using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(fileElement.fileContent)))
                {
                    request.InputStream = stream;
                    request.BucketName = bucket;
                    request.Key = fileElement.realName;
                    client.PutObject(request);
                }
                
            }
        }
        /// <summary>
        /// upload exported file to ftp server
        /// </summary>
        /// <param name="exportedFiles"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="remoteAddress"></param>
        public static void UploadFileToFTPServer(ExportCompleteData exportedFiles, string userName, string password,string host,string remoteAddress,int port = 21)
        {
            using (WebClient ftpClient = new WebClient())
            {
                ftpClient.Credentials = new NetworkCredential(userName, password);
                
                foreach (var fileElement in exportedFiles.data)
                {
                    byte[] fileContents = Convert.FromBase64String(fileElement.fileContent);
                    ftpClient.UploadData(string.Concat(host ,":",port, "/",remoteAddress,"/",fileElement.realName), fileContents);
                }


            }
            
        }
        public static string[] GetExportedFileNames(ExportCompleteData exportedFiles)
        {
            return exportedFiles.data.Select((ele) => ele.realName).ToArray();
        }
    }
}
