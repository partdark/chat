using chat.Models;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace chat.Services;

public class RedisChatMessageService : IChatMessageService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisChatMessageService> _logger;
    
  
    private readonly JsonSerializerSettings _serializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
        NullValueHandling = NullValueHandling.Ignore
    };
    
   
    private bool _redisAvailable = true;
    private DateTime _nextRedisRetry = DateTime.UtcNow;
    private readonly TimeSpan _redisRetryInterval = TimeSpan.FromMinutes(1);

    public RedisChatMessageService(IDistributedCache cache, ILogger<RedisChatMessageService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<ChatMessage>> GetChatHistoryAsync(string chatRoom)
    {
        try
        {
          
            var cachedHistory = await _cache.GetStringAsync($"Чат:{chatRoom}:история");
            
            if (string.IsNullOrEmpty(cachedHistory))
            {
                return new List<ChatMessage>();
            }
            
            var messages = JsonConvert.DeserializeObject<List<ChatMessage>>(cachedHistory, _serializerSettings) 
                ?? new List<ChatMessage>();
            
            
            _redisAvailable = true;
            
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения данных Redis");
            
           
            _redisAvailable = false;
            _nextRedisRetry = DateTime.UtcNow.Add(_redisRetryInterval);
            
            
            return new List<ChatMessage>();
        }
    }

    public async Task AddMessageAsync(string chatRoom, ChatMessage message)
    {
        try
        {
           
            var messages = await GetChatHistoryAsync(chatRoom);
            
            
            messages.Add(message);
            
           
            if (messages.Count > 50)
            {
                messages = messages.Skip(messages.Count - 50).ToList();
            }
            
            
            if (_redisAvailable || DateTime.UtcNow >= _nextRedisRetry)
            {
                
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                };
                
                await _cache.SetStringAsync(
                    $"Чат:{chatRoom}:история",
                    JsonConvert.SerializeObject(messages, _serializerSettings),
                    options
                );
                
               
                _redisAvailable = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения данных в Redis");
            
            
            _redisAvailable = false;
            _nextRedisRetry = DateTime.UtcNow.Add(_redisRetryInterval);
        }
    }
}