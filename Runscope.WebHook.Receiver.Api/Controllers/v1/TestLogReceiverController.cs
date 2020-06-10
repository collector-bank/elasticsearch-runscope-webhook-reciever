using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Runscope.WebHook.Receiver.Api.Controllers.v1
{
    public class TestLogReceiverController : ControllerBase
    {
        private readonly ILogger<TestLogReceiverController> _logger;
        private readonly string _apiKey;
        private readonly string _agentRegionName;
        private readonly ElasticConnector _elasticConnector;

        public TestLogReceiverController(ILogger<TestLogReceiverController> logger, ApiKey apiKey, AgentRegionName agentRegionName, ElasticConnector elasticConnector)
        {
            _logger = logger;
            _apiKey = apiKey.Key;
            _agentRegionName = agentRegionName.RegionName;
            _elasticConnector = elasticConnector;
        }

        [HttpPost]
        [Route("{apikey}")]
        public async Task<ActionResult> Post([FromBody] JsonElement body, [FromRoute] string apikey)
        {
            try
            {
                if (apikey != _apiKey)
                {
                    return Unauthorized();
                }

                var newtonBody = JObject.Parse(JsonSerializer.Serialize(body));

                var now = DateTime.Now;
                var documentsAsStrings = DataFunctions.ProcessRequestData(newtonBody, _agentRegionName, now, out DateTime testTime);

                var list = documentsAsStrings.Select(d => new ElasticMessage { Content = d, Date = testTime }).ToList();

                await _elasticConnector.BulkInsertToElastic(list);

                _logger.LogInformation("TestPost");

                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex);
            }
        }
    }
}