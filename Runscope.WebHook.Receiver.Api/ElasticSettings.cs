namespace Runscope.WebHook.Receiver.Api
{
    public class ElasticSettings
    {
        public string Cluster { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string IndexPrefix { get; set; }
    }
}
