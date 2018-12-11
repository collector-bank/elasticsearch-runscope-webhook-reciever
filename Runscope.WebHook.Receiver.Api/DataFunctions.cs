using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Runscope.WebHook.Receiver.Api
{
    public static class DataFunctions
    {
        public static string[] ProcessRequestData(JObject requestBody, DateTime now)
        {
            var resultDocuments = new List<string>();

            if (requestBody["messages"] != null)
            {
                requestBody["message"] = JToken.FromObject(requestBody["messages"].Last);
                requestBody.Remove("messages");
            }
            if (requestBody["requests"] != null)
            {
                requestBody["request"] = JToken.FromObject(requestBody["requests"].Last);
                requestBody.Remove("requests");
            }
            if (requestBody["started_at"] != null)
            {
                requestBody["started_at"] = ConvertTimeFromTicksToDateTime(requestBody, "started_at");
            }
            if (requestBody["finished_at"] != null)
            {
                requestBody["finished_at"] = ConvertTimeFromTicksToDateTime(requestBody, "finished_at");
            }
            if (requestBody["reported_at"] != null)
            {
                requestBody["reported_at"] = now.ToString("o");
            }
            if (requestBody["region_name"] != null)
            {
                requestBody["region_name"] = PrettifyRegionName(requestBody["region_name"].ToString());
            }
            if (requestBody["variables"] != null && requestBody["variables"]["result"] != null && requestBody["variables"]["jsonresult"] == null)
            {
                if (TryParseJsonObject(requestBody["variables"]["result"].ToString(), out JObject result))
                {
                    requestBody["variables"]["jsonresult"] = result;
                    ((JObject)requestBody["variables"]).Remove("result");
                }
            }

            RemoveSensitiveVariables(requestBody);

            resultDocuments.Add(JsonConvert.SerializeObject(requestBody));
            resultDocuments.AddRange(HeartbeatResultDocumentProcessor.SplitSubComponentResultsIntoSeparateDocuments(requestBody).Select(JsonConvert.SerializeObject));
            return resultDocuments.ToArray();
        }

        public static void RemoveSensitiveVariables(JObject jobject)
        {
            string[] removeSensitive = jobject
                .DescendantsAndSelf()
                .Where(o => o is JObject && o["removesensitive"] != null)
                .SelectMany(o => o["removesensitive"].ToString().Split(','))
                .Concat(new[] { "removesensitive" })
                .ToArray();

            JToken[] sensitiveVariables = jobject
                .DescendantsAndSelf()
                .Select(o => o as JProperty)
                .Where(p => p != null && removeSensitive.Contains(p.Name))
                .ToArray();

            foreach (var sensitiveVariable in sensitiveVariables)
            {
                sensitiveVariable.Remove();
            }
        }

        public static string PrettifyRegionName(string regionName)
        {
            dynamic settings = JObject.Parse(System.IO.File.ReadAllText(System.IO.File.Exists("appsettings.Development.json") ? "appsettings.Development.json" : "appsettings.json"));

            var agentRegionName = settings.AgentRegionName;

            if (regionName == "None - None")
            {
                return agentRegionName;
            }
            else if (regionName.EndsWith(" - None"))
            {
                return regionName.Substring(0, regionName.Length - 7);
            }
            else if (regionName.Contains(" - "))
            {
                return regionName.Substring(regionName.IndexOf(" - ") + 3);
            }

            return regionName;
        }

        private static bool TryParseJsonObject(string json, out JObject jobject)
        {
            try
            {
                jobject = JObject.Parse(json);
            }
            catch
            {
                jobject = null;
                return false;
            }

            return true;
        }

        public static string ConvertTimeFromTicksToDateTime(JObject requestBody, string path)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds((double)requestBody[path]).ToString("o");
        }
    }
}
