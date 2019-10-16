/* Send FusionExport files as attachments via mail

 * Sending email using System.Net.Mail

 * Provide your SMTP details for settiing up (line no. 42 & 43).

 * This sample was tested using Gmail SMTP configuration

 * Finally, provide email metadata (details like subject, to, from) while sending email (line no. 47 to 50).
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using FusionCharts.FusionExport.Client; // Import sdk

namespace examples
{

    public static class SendEmail
    {
        public static void Run(string host = Constants.DEFAULT_HOST, int port = Constants.DEFAULT_PORT)
        {
            // Instantiate the ExportConfig class and add the required configurations
            ExportConfig exportConfig = new ExportConfig();
            List<string> results = new List<string>();

            // Instantiate the ExportManager class
            using (ExportManager exportManager = new ExportManager())
            {
                exportConfig.Set("chartConfig", File.ReadAllText("./resources/dashboard_charts.json"));
                exportConfig.Set("templateFilePath", "./resources/template.html");
                // Call the Export() method with the export config
                results.AddRange(exportManager.Export(exportConfig));
            }

            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmaill.com");
                SmtpServer.Port = 587;
                SmtpServer.Credentials = new System.Net.NetworkCredential("rahul@fusioncharts.com", "R@hul@2018#");
                SmtpServer.EnableSsl = true;

                mail.From = new MailAddress("<SENDER'S EMAIL>");
                mail.To.Add("<RECEIVERS'S EMAIL>");
                mail.Subject = "FusionExport";
                mail.Body = "Hello,\n\nKindly find the attachment of FusionExport exported files.\n\nThank you!";

                System.Net.Mail.Attachment attachment;

                results.ForEach(i =>
                {
                    attachment = new System.Net.Mail.Attachment(i);
                    mail.Attachments.Add(attachment);
                });

                SmtpServer.Send(mail);
                Console.WriteLine("FusionExport C# Client: Email Sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message + " " + ex.InnerException.Message);
            }
        }
    }
}
