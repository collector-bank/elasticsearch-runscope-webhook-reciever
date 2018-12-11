using System.Collections.Generic;

namespace Runscope.WebHook.Receiver.Api
{
    public interface IElasticsearchConnector<T>
    {
        void BulkInsertToElastic(IEnumerable<T> objects, string index);
    }
}
