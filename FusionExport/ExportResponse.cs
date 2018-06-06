using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace FusionCharts.FusionExport.Client
{
    public class ExportCompleteData
    {
        public List<ExportCompleteDataElement> data;

        public static ExportCompleteData FromResponseString(string responseString)
        {
            return JsonConvert.DeserializeObject<ExportCompleteData>(responseString);
        }
    }

    public class ExportCompleteDataElement
    {
        public string realName;
        public string fileContent;
    }

    public class StateChangeData
    {
        public string reporter;
        public int weight;
        public string customMsg;
        public string uuid;

        public static StateChangeData FromStateString(string stateString)
        {
            return JsonConvert.DeserializeObject<StateChangeData>(stateString);
        }
    }

    public class ExportEvent
    {
        public StateChangeData state;
        public ExportCompleteData exportedFiles;
    }
}