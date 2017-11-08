using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FusionCharts.FusionExport.Client
{
    public delegate void ExportDoneEventHandler(object sender, ExportDoneEventArgs e);

    public class ExportDoneEventArgs : EventArgs
    {
        private string result;
        private Exception error;

        public ExportDoneEventArgs(string result, Exception error)
        {
            this.result = result;
            this.error = error;
        }

        public string Result
        {
            get { return result; }
        }

        public Exception Error
        {
            get { return error; }
        }
    }
}
