using StackExchange.Redis;
using System.Text.Json;
namespace Redis.Services
{
    public class CacheService : ICacheService
    {
        IDatabase _CasheDb;
        public CacheService()
        {
            var redis = ConnectionMultiplexer.Connect("localhost:6379");  // connect with cashing redis using port number in running downloader redis
            _CasheDb = redis.GetDatabase();
        }

        public T GetData<T>(string key)
        {
            var value = _CasheDb.StringGet(key); // get data as string from cashing redis
            if(!string.IsNullOrEmpty(value))
                return JsonSerializer.Deserialize<T>(value); // convert data as string to generic object

            return default;
        }

        public object RemoveData(string key)
        {
            var _exist = _CasheDb.KeyExists(key); // check data with this key is found or not
            if (_exist)
                return _CasheDb.KeyDelete(key);    // delete key of data in redis
            return false;
        }

        public bool SetData<T>(string key, T value, DateTimeOffset expirationTime)
        {
            var expirytime = expirationTime.DateTime.Subtract(DateTime.Now);// calculate difference between expiredtime and currenttime
            return _CasheDb.StringSet(key,JsonSerializer.Serialize(value),expirytime);  // add key data into redis
        }
    }
}
