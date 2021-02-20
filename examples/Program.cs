using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FusionExportTest;

namespace examples
{
    class Program
    {
        static void Main(string[] args)
        {
            //TimeSeries.Run();

            //SendEmail.Run();
            //Header_Footer.Run();
            //ExportSingleChart.Run();
            //BulkExport.Run();
            //Amcharts_Exp.Run();
            //AsyncCapture.Run();
            //Chartjs_Exp.Run();
            //ConvertSvg.Run();
            //D3_Exp.Run();
            //Dashboard.Run();
            //ExportAsStream.Run();
            //ExportMultipleCharts.Run();
            //DashboardLogoHead.Run();
            //GoogleCharts_Exp.Run();
            //Highcharts_Exp.Run();
            //InjectJsCallback.Run();
            //OutputAsZip.Run();
            //Quality.Run();
            //ZingChart_Exp.Run();
            //ExportLocalFont.Run();
            //ExcelExportCSV.Run();
            //ExcelExportXls.Run();
            //ExcelExportMultiple.Run();
            //ExcelExportMultiple2.Run();
            //ExportUsingDefaultTemplates.Run();
            ExportSingleViaHttps.Run("127.0.0.1", 1337, false);
            Console.ReadKey();
        }
    }
}
