using System.Text.Json;
using MatchMaking.Service.Models;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace MatchMaking.Service.Controllers;

[ApiController]
    [Route("matchmaking")]
    public class MatchMakingController : ControllerBase
    {
        private readonly IDatabase _redisDb;

        public MatchMakingController(IConnectionMultiplexer redis)
        {
            _redisDb = redis.GetDatabase();
        }

        [HttpPost("search")]
        public async Task<IActionResult> MatchSearchRequest([FromQuery] string userId)
        {
            throw new NotImplementedException();
        }

        [HttpGet("matchinfo")]
        public async Task<IActionResult> RetrieveMatchInformation([FromQuery] string userId)
        {
            throw new NotImplementedException();
        }
    }