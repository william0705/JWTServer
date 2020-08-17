using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace JWTServer.CustomMiddleware
{
    /// <summary>
    /// 自定义中间件-请求限流中间件
    /// </summary>
    public class RequestRateLimitMiddleware
    {
        private readonly RequestDelegate _requestDelegate;
        private readonly IMemoryCache _memoryCache;
        private readonly int Limit = 3;

        public RequestRateLimitMiddleware(RequestDelegate requestDelegate,IMemoryCache memoryCache)
        {
            _requestDelegate = requestDelegate;
            _memoryCache = memoryCache;
        }

        public async Task Invoke(HttpContext context)
        {
            var requestKey = $"{context.Request.Method}-{context.Request.Path}";
            var cacheOptions=new MemoryCacheEntryOptions()
            {
                AbsoluteExpiration = DateTime.Now.AddMinutes(1)
            };//缓存时间一分钟

            if (_memoryCache.TryGetValue(requestKey, out int hitCount))
            {
                if (hitCount < Limit)
                {
                    await ProcessRequest(context, requestKey, hitCount, cacheOptions);
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.Request.Headers["X-RateLimit-RetryAfter"]=cacheOptions.AbsoluteExpiration?.ToString();
                }
            }
            else
            {
                await ProcessRequest(context,requestKey,hitCount,cacheOptions);
            }
        }

        private async Task ProcessRequest(HttpContext context, string requestKey, int hitCount, MemoryCacheEntryOptions cacheOptions)
        {
            hitCount++;
            _memoryCache.Set(requestKey, hitCount, cacheOptions);
            context.Response.Headers["X-RateLimit-Limit"] = Limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = (Limit - hitCount).ToString();

            await _requestDelegate.Invoke(context);
        }
    }
}
