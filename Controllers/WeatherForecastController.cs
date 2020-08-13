using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JWTServer.Controllers
{
    /// <summary>
    /// IDataProtectionProvider数据保护API(详细参考书籍RestfulAPI)
    /// </summary>
    //[Authorize]
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IDataProtectionProvider _protectionProvider;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,IDataProtectionProvider protectionProvider)
        {
            _logger = logger;
            _protectionProvider = protectionProvider;
        }

        [HttpGet("WeatherForecasts")]
        public IEnumerable<WeatherForecast> Get()
        {
            var protection = _protectionProvider.CreateProtector("ProtectedId");
            var rng = new Random();
            var id = 1;
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Id= protection.Protect((id++).ToString()),
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("/WeatherForecast/{id}")]
        public IActionResult Get(string id)
        {
            var protection = _protectionProvider.CreateProtector("ProtectedId");
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            var protectedId = protection.Unprotect(id);
            return Ok(protectedId);
        }
    }
}
