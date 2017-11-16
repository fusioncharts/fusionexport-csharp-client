using System;
using System.Collections.Generic;
using System.Text;

namespace FusionCharts.FusionExport.Client
{
    public class ExportException : Exception
    {
        public ExportException() : base() { }

        public ExportException(string message) : base(message) { }

        public ExportException(string message, Exception e) : base(message, e) { }
    }
}
