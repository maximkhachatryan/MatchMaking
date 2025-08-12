using System.Text.Json;
using Confluent.Kafka;
using MatchMaking.Common.Constants;
using MatchMaking.Common.Messages;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace MatchMaking.Service.Controllers;

[ApiController]
[Route("matchmaking")]
public class MatchMakingController : ControllerBase
{
    private readonly IDatabase _redisDb;
    private readonly IProducer<Null, string> _kafkaProducer;

    public MatchMakingController(IConnectionMultiplexer redis, IProducer<Null, string> kafkaProducer)
    {
        _redisDb = redis.GetDatabase();
        _kafkaProducer = kafkaProducer;
    }

    [HttpPost("search/{userId}")]
    public async Task<IActionResult> MatchSearchRequest([FromRoute] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("userId is required.");
        
        //TODO: Check if user is waiting for a match, don't allow for a new request

        var messagePayload = JsonSerializer.Serialize(new MatchMakingRequestMessage(userId));

        try
        {
            var message = new Message<Null, string> { Value = messagePayload };
            await _kafkaProducer.ProduceAsync(KafkaTopics.KafkaRequestTopic, message);
            
            await _redisDb.SetAddAsync("waiting:users", userId);

            return Ok(new { Status = "Message sent"});
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error sending message: {ex.Message}");
        }
    }
    
    [HttpGet("matchinfo/userId/{userId}")]
    public async Task<IActionResult> RetrieveMatchInformation([FromRoute] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("userId is required.");

        var matchId = await _redisDb.HashGetAsync("user_match_map", userId);
        if (matchId.IsNullOrEmpty)
            return NotFound();

        var userIds = await _redisDb.ListRangeAsync($"match:{matchId}:users");

        var response = new
        {
            matchId = matchId.ToString(),
            userIds = userIds.Select(u => u.ToString()).ToList()
        };

        return Ok(response);
    }
}