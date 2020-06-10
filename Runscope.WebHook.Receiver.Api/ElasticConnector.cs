using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Runscope.WebHook.Receiver.Api
{
    public class ElasticConnector
    {
        private readonly ElasticSettings _elasticSettings;
        private readonly HttpClient _httpClient;

        public ElasticConnector(IOptions<ElasticSettings> elasticSettings, HttpClient httpClient)
        {
            _elasticSettings = elasticSettings.Value;
            _httpClient = httpClient;
        }

        public async Task BulkInsertToElastic(IEnumerable<ElasticMessage> documents)
        {
            var bulkbody = new StringBuilder();

            foreach (var document in documents)
            {
                string metadata = "{ \"index\": { \"_index\": \"" + $"{_elasticSettings.IndexPrefix}{document.Date:yyyy.MM}" + "\" } }";
                bulkbody.AppendLine(metadata);
                bulkbody.AppendLine(document.Content.Replace("\r", string.Empty).Replace("\n", string.Empty));
            }

            await InsertBulkRows(bulkbody.ToString());
        }

        private async Task InsertBulkRows(string bulkbody)
        {
            if (_elasticSettings.Username != string.Empty && _elasticSettings.Password != string.Empty)
            {
                string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_elasticSettings.Username}:{_elasticSettings.Password}"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            }
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new StringContent(bulkbody, Encoding.UTF8, "application/x-ndjson");
            // Elastic doesn't support setting charset (after encoding at Content-Type), blank it out.
            content.Headers.ContentType.CharSet = string.Empty;
            var address = new Uri($"{_elasticSettings.Cluster}/_bulk");
            var response = await _httpClient.PostAsync(address, content);
            response.EnsureSuccessStatusCode();
        }
    }
}
