using System;

namespace FusionCharts.FusionExport.Client
{
    public class FusionExportAmazonS3Exception : Exception
    {
        
        public FusionExportAmazonS3Exception()
            : base()
        { }

        public FusionExportAmazonS3Exception(string message)
            : base(message)
        { }

        public FusionExportAmazonS3Exception(string message, Exception innerException)
            : base(message, innerException)
        { }

        
    }
    public class FusionExportFTPServerException : Exception
    {

        public FusionExportFTPServerException()
            : base()
        { }

        public FusionExportFTPServerException(string message)
            : base(message)
        { }

        public FusionExportFTPServerException(string message, Exception innerException)
            : base(message, innerException)
        { }


    }
}
