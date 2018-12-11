using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Runscope.WebHook.Receiver.Api
{
    internal class HeartbeatResultDocumentProcessor
    {
        internal static IEnumerable<JObject> SplitSubComponentResultsIntoSeparateDocuments(JObject requestBodyIn)
        {
            dynamic requestBody = requestBodyIn;
            var componentResults = requestBody?.variables?.jsonresult?.ComponentResults;
            if (componentResults?.Type != JTokenType.Array)
            {
                yield break;
            }

            var systemId = GetSystemId(requestBody);
            foreach (JToken componenResult in componentResults.Children())
            {
                JObject result = new JObject
                {
                    new JProperty("document_type", "HeartbeatSubcomponentResult"),
                    new JProperty("system_id", systemId),
                    { "started_at", requestBody?.started_at },
                    { "finished_at", requestBody?.finished_at }
                };

                JObject variables = new JObject();
                foreach (var child in componenResult.Children())
                {
                    variables.Add(child);
                }
                result.Add("variables", variables);

                yield return result;
            }
        }

        private static string GetSystemId(dynamic requestBody)
        {
            return string.Join(".", new[] {
                requestBody?.team_name,
                requestBody?.bucket_name,
                requestBody?.environment_name,
                requestBody?.test_name,
            }.Where(x => x != null));
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
    }
}