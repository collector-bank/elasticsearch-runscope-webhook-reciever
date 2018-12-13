using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Runscope.WebHook.Receiver.Api
{
    [ApiController]
    public class TestLogRecieverController : ControllerBase
    {
        private readonly List<ElasticLowLevelConnector> _testLogReceivers = new List<ElasticLowLevelConnector>();
        private readonly string _apiKey;
        private readonly List<string> _indexPrefixes = new List<string>();
        private dynamic settings;

        public TestLogRecieverController()
        {
            settings = JObject.Parse(System.IO.File.ReadAllText(System.IO.File.Exists("appsettings.Development.json") ? "appsettings.Development.json" : "appsettings.json"));

            var clusters = settings.ElasticsearchClusters;
            var usernames = settings.ElasticsearchUsernames;
            var passwords = settings.ElasticsearchPasswords;
            var indexprefixes = settings.ElasticsearchIndexPrefixes;

            _indexPrefixes.AddRange(indexprefixes.Split(','));


            for (var cluster = 0; cluster < clusters.Split(',').Length; cluster++)
            {
                _testLogReceivers.Add(new ElasticLowLevelConnector(clusters.Split(',')[cluster],
                    GetIndexedOrFirst(usernames.Split(',').ToList(), cluster),
                    GetIndexedOrFirst(passwords.Split(',').ToList(), cluster)));
            }

            _apiKey = settings.ApiKey;
        }

        [HttpPost]
        [Route("{apikey}")]
        public ActionResult Post([FromBody] JObject body, [FromQuery] string apikey)
        {
            var agentRegionName = settings.AgentRegionName;

            try
            {
                if (apikey != _apiKey)
                {
                    return Unauthorized();
                }

                var now = DateTime.Now;
                var documentsAsStrings = DataFunctions.ProcessRequestData(body, now, agentRegionName);

                for (var cluster = 0; cluster < _testLogReceivers.Count; cluster++)
                {
                    var receiver = _testLogReceivers[cluster];
                    var indexPrefix = GetIndexedOrFirst(_indexPrefixes, cluster);
                    var list = new List<LowLevelMessage>();

                    foreach (var documentAsString in documentsAsStrings)
                    {
                        list.Add(new LowLevelMessage
                        {
                            Body = documentAsString,
                            Index = $"{indexPrefix}{now:yyyy.MM}",
                            Type = "runscope"
                        });
                    }

                    try
                    {
                        receiver.BulkInsertToElastic(list);
                    }
                    catch
                    {
                        // Allow any target cluster to fail, allows for shutdown of any arbitrary cluster while still writing to the those that are still online.
                    }
                }

                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return new ContentResult { StatusCode = StatusCodes.Status500InternalServerError, Content = ex.ToString() };
            }
        }

        private string GetIndexedOrFirst(List<string> list, int index)
        {
            return index < list.Count ? list[index] : list[0];
        }
    }
}