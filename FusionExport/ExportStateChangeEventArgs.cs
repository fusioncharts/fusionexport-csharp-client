using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FusionCharts.FusionExport.Client
{
    public delegate void ExportStateChangeEventHandler(object sender, ExportStateChangeEventArgs e);

    public class ExportStateChangeEventArgs : EventArgs
    {
        private string meta;

        public ExportStateChangeEventArgs(string meta)
        {
            this.meta = meta;
        }

        public string Meta
        {
            get { return meta; }
        }
    }
}
