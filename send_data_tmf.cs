using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RestSharp;
using System.Xml;
using System.Net.Http;
using System.Net;
using System.Text.Unicode;
using Newtonsoft.Json;

namespace senddata_tmf_sap
{
    public static class send_data_tmf
    {
        [FunctionName("send_data_tmf")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var processStatus = String.Empty;
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var xmlDoc = new XmlDocument();
            XmlDocument tmfDoc = new XmlDocument();
            var tmfInput = string.Empty;
            var tmfJsonResponse = string.Empty;
            var tmfMulInput = string.Empty;
            var retunCount = 0;
            xmlDoc.LoadXml(requestBody);
            if (req.ContentLength > 500)
            {

                var start = xmlDoc.InnerText.IndexOf("{") + 1;
                var end = xmlDoc.InnerText.LastIndexOf("}") - start;
                var idproex = xmlDoc.InnerText.Substring(start, end);
                string[] tmfData = idproex.Trim().Split(' ');
                start = xmlDoc.InnerXml.IndexOf("<ES_IDPROC>")+12;
                end = xmlDoc.InnerXml.IndexOf("</ES_IDPROC>") - start;
                idproex = xmlDoc.InnerXml.Substring(start, end);
                foreach (string tdata in tmfData)
                {
                    if (tdata.Contains("FILENAME"))
                    {
                        tmfInput = "<item>\r\n" + tdata.Replace("FILENAME=", "<filename>") + "</filename>\r\n";
                    }
                    else if (tdata.Contains("TABLENAME"))
                    {
                        tmfInput = tdata.Replace("TABLENAME=", "<tablename>") + "</tablename>\r\n";
                        retunCount++;
                    }
                    else if (tdata.Contains("CONTENT"))
                    {

                        tmfInput = tdata.Replace("CONTENT=", "<content>") + "</content> </item>\r\n ";
                    }

                    tmfMulInput += tmfInput;
                    tmfInput = string.Empty;
                }
                tmfDoc.LoadXml("<root>\r\n" +
                              "<items>\r\n" +
                                   tmfMulInput +
                                  "</items>\r\n" +
                            "</root>\r\n");

                processStatus = postTMFData(tmfDoc.InnerXml);
                
                start = processStatus.IndexOf("<process_status>")+16;
                end = processStatus.IndexOf("</process_status>") - start;
              // processStatus = processStatus.Substring(start, end) + result + "\r\n <SentTablesCount>" + retunCount + "</SentTablesCount>";
              var tmfResponse = new
                {
                    IDPROEX = idproex,
                    PROCESS_STATUS = processStatus.Substring(start, end),
                    SentTablesCount =retunCount
                };

                //Tranform it to Json object
                 tmfJsonResponse = JsonConvert.SerializeObject(tmfResponse);

            }
            else
            {
                var tmfResponse = new
                {
                    IDPROEX = "0000000000",
                    PROCESS_STATUS = "s"
                };

                //Tranform it to Json object
                 tmfJsonResponse =  JsonConvert.SerializeObject(tmfResponse);
            }

            return new JsonResult(tmfJsonResponse);
        }
    

    public static string postTMFData(string innner)
    {
        try
        {
            var client = new RestClient(Environment.GetEnvironmentVariable("TMF_URI"));
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", Environment.GetEnvironmentVariable("TMF_Auth"));
            request.AddHeader("Content-Type", "application/xml");
            request.AddParameter("application/xml", innner, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            //Console.WriteLine(response.Content);
            return response.Content;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
    }
