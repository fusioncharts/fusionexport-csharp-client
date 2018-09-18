using System;

namespace FusionCharts.FusionExport.Client
{
    public class FusionExportHttpException : Exception
    {

        public FusionExportHttpException()
            : base()
        { }

        public FusionExportHttpException(string message)
            : base(message)
        { }

        public FusionExportHttpException(string message, Exception innerException)
            : base(message, innerException)
        { }


    }
}
