using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FusionCharts.FusionExport.Client
{
    public delegate void ExportStateChangeEventHandler(object sender, ExportStateChangeEventArgs e);

    public class ExportStateChangeEventArgs : EventArgs
    {
        private string state;

        public ExportStateChangeEventArgs(string state)
        {
            this.state = state;
        }

        public string State
        {
            get { return state; }
        }
    }
}
