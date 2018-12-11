using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elasticsearch.Net;
using Nest;
using Newtonsoft.Json;

namespace Runscope.WebHook.Receiver.Api
{
    public class ElasticLowLevelConnector
    {
        private readonly ElasticLowLevelClient _client;

        public ElasticLowLevelConnector(string node, string username, string password)
        {
            var uri = new Uri(node);

            var singleNodeConnectionPool = new SingleNodeConnectionPool(uri);
            var settings = new ConnectionSettings(singleNodeConnectionPool);
            settings.RequestTimeout(new TimeSpan(0, 0, 0, 0, 90000));
            settings.BasicAuthentication(username, password);
            _client = new ElasticLowLevelClient(settings);
        }

        public void BulkInsertToElastic(IEnumerable<LowLevelMessage> objects)
        {
            var payloads = new List<string>();

            foreach (var lowLevelMessage in objects)
            {
                var action = new { index = new { _index = lowLevelMessage.Index, _type = lowLevelMessage.Type } };
                payloads.Add(JsonConvert.SerializeObject(action, Formatting.None));
                payloads.Add(lowLevelMessage.Body);
            }

            var response = _client.Bulk<DynamicResponse>(PostData.MultiJson(payloads));
            Debug.WriteLine(response);
        }
    }

    public class LowLevelMessage
    {
        public string Index { get; set; }

        public string Type { get; set; }

        public string Body { get; set; }
    }
}
