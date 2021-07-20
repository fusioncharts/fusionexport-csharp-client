using System;
using FusionCharts.FusionExport.Client;
using System.Collections.Generic;

namespace FusionExportExecutable
{

    class test
    {
        static void Main(string[] args)
        {
            try
            {
                List<string> results = new List<string>();
                string chartConfig = @"[{
                        ""type"": ""column2d"",
                        ""renderAt"": ""chart-container"",
                        ""width"": ""600"",
                        ""height"": ""400"",
                        ""dataFormat"": ""json"",
                        ""dataSource"": {
                            ""chart"": {
                                ""caption"": ""Number of visitors last week"",
                                ""subCaption"": ""Bakersfield Central vs Los Angeles Topanga""
                            },
                            ""data"": [{
                                    ""label"": ""Mon"",
                                    ""value"": ""15123""
                                },{
                                    ""label"": ""Tue"",
                                    ""value"": ""14233""
                                },{
                                    ""label"": ""Wed"",
                                    ""value"": ""25507""
                                }
                            ]
                        }
                    },
                    {
                        ""type"": ""column2d"",
                        ""renderAt"": ""chart-container"",
                        ""width"": ""600"",
                        ""height"": ""400"",
                        ""dataFormat"": ""json"",
                        ""dataSource"": {
                            ""chart"": {
                                ""caption"": ""Number of visitors last week"",
                                ""subCaption"": ""Bakersfield Central vs Los Angeles Topanga""
                            },
                            ""data"": [{
                                    ""label"": ""Mon"",
                                    ""value"": ""15123""
                                },{
                                    ""label"": ""Tue"",
                                    ""value"": ""14233""
                                },{
                                    ""label"": ""Wed"",
                                    ""value"": ""25507""
                                }
                            ]
                        }
                    }]";


                ExportConfig exportConfig = new ExportConfig();

                using (ExportManager em = new ExportManager())
                {
                    exportConfig.Set("chartConfig", chartConfig);
                    exportConfig.Set("type", "xlsx");
                    results.AddRange(em.Export(exportConfig));
                }

                foreach (string path in results)
                {
                    Console.WriteLine(path);
                    Console.WriteLine("adasd");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.Read();
        }
    }
}
