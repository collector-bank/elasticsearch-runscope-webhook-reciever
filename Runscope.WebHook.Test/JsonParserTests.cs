using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Runscope.WebHook.Receiver.Api;
using System;
using System.IO;
using System.Reflection;

namespace Runscope.WebHook.Test
{
    public class JsonParserTests
    {
        private string GetJsonContent(string filename)
        {
            if (File.Exists(filename))
            {
                return File.ReadAllText(filename);
            }
            else
            {
                var assembly = Assembly.GetExecutingAssembly();

                using (Stream stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{filename}"))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        [Test]
        public void TestLastRequest()
        {
            string content = GetJsonContent("payload.json");
            var payload = JObject.Parse(content);
            ((JObject)payload["variables"]).Remove("result");
            var result = DataFunctions.ProcessRequestData(payload, DateTime.Now);
            Assert.AreEqual(1, result.Length);
            var jsonresult = JObject.Parse(result[0]);
            Assert.AreEqual(null, jsonresult["requests"]);
            Assert.AreNotEqual(null, jsonresult["request"]);
            Assert.AreEqual("https://api.runscope2.com/", jsonresult["request"]["url"].Value<string>());
        }


        [TestCase("US East - Northern Virginia", "Northern Virginia")]
        [TestCase("None - None", "MegaCityOne")]
        public void TestRegionNamePrettification(string regionName, string expectedPrettified)
        {
            Assert.AreEqual(expectedPrettified, DataFunctions.PrettifyRegionName(regionName));
        }

        [Test]
        public void TestProcessRequestDataWithHeartbeatResultShouldReturnMultipleDocuments()
        {
            string content = GetJsonContent("payload.json");
            var payload = JObject.Parse(content);
            string[] result = DataFunctions.ProcessRequestData(payload, DateTime.Now);
            Assert.AreEqual(6, result.Length);
        }

        [Test]
        public void TestProcessRequestDataWithoutHeartbeatResultShouldReturnOneDocument()
        {
            string content = GetJsonContent("payload.json");
            var payload = JObject.Parse(content);
            ((JObject)payload["variables"]).Remove("result");
            string[] result = DataFunctions.ProcessRequestData(payload, DateTime.Now);
            Assert.AreEqual(1, result.Length);
        }

        [Test]
        public void TestRemoveSensitiveVariables()
        {
            string content = GetJsonContent("payload.json");

            var payload1 = JObject.Parse(content);
            payload1["removesensitive"] = "aa,bb,cc";

            payload1["initial_variables"]["aa"] = 111;
            payload1["requests"][0]["variables"]["bb"] = 222;
            payload1["variables"]["cc"] = 333;

            payload1["initial_variables"]["dd"] = 444;
            payload1["requests"][0]["variables"]["ee"] = 555;
            payload1["variables"]["ff"] = 666;

            var payload2 = JObject.Parse(content);

            payload2["initial_variables"]["dd"] = 444;
            payload2["requests"][0]["variables"]["ee"] = 555;
            payload2["variables"]["ff"] = 666;

            DataFunctions.RemoveSensitiveVariables(payload1);
            Assert.AreEqual(payload1.ToString(), payload2.ToString());
        }
    }
}
