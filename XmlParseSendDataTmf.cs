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

namespace ZTMFBR_AF_Xml_SendData_Tmf
{
    public static class XmlParseSendDataTmf
    {
        [FunctionName("XmlParseSendDataTmf")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var tmfResponse =String.Empty;
            var xmlDoc = new XmlDocument();
            XmlDocument tmfDoc = new XmlDocument();
            var tmfInput = string.Empty;
            xmlDoc.LoadXml(requestBody);
            var start = xmlDoc.InnerText.IndexOf("{") + 1;
            var end = xmlDoc.InnerText.LastIndexOf("}") - start;
            var result = xmlDoc.InnerText.Substring(start, end);

            string[] tmfData = result.Trim().Split(' ');
            start = xmlDoc.InnerXml.IndexOf("<ES_IDPROC>");
            end = xmlDoc.InnerXml.IndexOf("</ES_IDPROC>")-start+12;
            result = xmlDoc.InnerXml.Substring(start,end).Replace("ES_IDPROC", "IDPROEX");
            foreach (string tdata in tmfData)
            {
                if (tdata.Contains("FILENAME"))
                {
                    tmfInput = tdata.Replace("FILENAME=", "<filename>") + "</filename>";
                }
                else if (tdata.Contains("TABLENAME"))
                {
                    tmfInput += tdata.Replace("TABLENAME=", "<tablename>") + "</tablename>";
                }
                else if (tdata.Contains("CONTENT"))
                {

                    tmfInput += tdata.Replace("CONTENT=", "<content>") + "</content>";
                }
            }
            tmfDoc.LoadXml("<root>" +
                          "<items>" +
                              "<item>" + tmfInput +
                              "</item>" +
                          "</items>" +
                        "</root>");

            tmfResponse= postTMFData(tmfDoc.InnerXml);
            start = tmfResponse.IndexOf("<process_status>");
            end = tmfResponse.IndexOf("</process_status>") - start +17;
            tmfResponse = tmfResponse.Substring(start, end).Replace("process_status", "PROCESS_STATUS")+result;
            return new OkObjectResult(tmfResponse);

        }
        public static string postTMFData(string innner)
        {
            try
            {
                var client = new RestClient(Environment.GetEnvironmentVariable("TMF_URI"));
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", Environment.GetEnvironmentVariable("TMF_Auth"));
                request.AddHeader("Content-Type", "text/plain");
                request.AddParameter("text/plain", innner, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                Console.WriteLine(response.Content);
                return response.Content;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
