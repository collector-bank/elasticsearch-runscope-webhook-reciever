using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Runscope.WebHook.Receiver.Api
{
    [ApiController]
    public class TestLogRecieverController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly string _apiKey;
        private readonly string _agentRegionName;
        private readonly ElasticLowLevelConnector[] _testLogReceivers;

        public TestLogRecieverController(ILogger logger, ApiKey apiKey, AgentRegionName agentRegionName, ElasticLowLevelConnector[] TestLogReceivers)
        {
            _logger = logger;
            _apiKey = apiKey.Key;
            _agentRegionName = agentRegionName.RegionName;
            _testLogReceivers = TestLogReceivers;
        }

        [HttpPost]
        [Route("{apikey}")]
        public ActionResult Post([FromBody] JObject body, [FromRoute] string apikey)
        {
            _logger.Information("Request: {apikey} {body}", apikey, body);

            try
            {
                if (apikey != _apiKey)
                {
                    return Unauthorized();
                }

                DateTime now = DateTime.Now;

                var documentsAsStrings = DataFunctions.ProcessRequestData(body, _agentRegionName, now, out DateTime testTime);

                for (var cluster = 0; cluster < _testLogReceivers.Length; cluster++)
                {
                    var receiver = _testLogReceivers[cluster];
                    var list = new List<LowLevelMessage>();

                    foreach (var documentAsString in documentsAsStrings)
                    {
                        list.Add(new LowLevelMessage
                        {
                            Body = documentAsString,
                            Date = testTime,
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

                _logger.Information("Result ok");

                return Ok();
            }
            catch (ArgumentException ex)
            {
                _logger.Error("{exception}", ex);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error("{exception}", ex);
                return new ContentResult { StatusCode = StatusCodes.Status500InternalServerError, Content = ex.ToString() };
            }
        }
    }
}