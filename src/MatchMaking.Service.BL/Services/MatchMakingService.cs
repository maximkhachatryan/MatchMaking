using System.Text.Json;
using Confluent.Kafka;
using MatchMaking.Common.Constants;
using MatchMaking.Common.Messages;
using MatchMaking.Service.BL.Abstraction.Services;
using MatchMaking.Service.BL.Constants;
using MatchMaking.Service.BL.Models;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MatchMaking.Service.BL.Services;

public class MatchMakingService : IMatchMakingService
{
    private readonly ILogger<MatchMakingService> _logger;
    private readonly IDatabase _db;
    private readonly IProducer<Null, string> _producer;

    public MatchMakingService(
        ILogger<MatchMakingService> logger,
        IConnectionMultiplexer redis,
        IProducer<Null, string> producer)
    {
        _logger = logger;
        _db = redis.GetDatabase();
        _producer = producer;
    }

    public async Task SearchMatch(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new InvalidOperationException("UserId is required.");
            var isWaiting = await _db.SetContainsAsync(MatchMakingServiceRedisKeys.WaitingUsersSetKey, userId);
            if (isWaiting)
                throw new InvalidOperationException("User has already sent a request and waiting for a match");

            var messagePayload = JsonSerializer.Serialize(new MatchMakingRequestMessage(userId));
            var message = new Message<Null, string> { Value = messagePayload };
            await _producer.ProduceAsync(KafkaTopics.KafkaRequestTopic, message);

            await _db.SetAddAsync(MatchMakingServiceRedisKeys.WaitingUsersSetKey, userId);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Error while sending matchmaking request", ex);
        }
    }

    public async Task<MatchDto> RetrieveMatchInformation(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new InvalidOperationException("UserId is required.");

            var matchId = await _db.HashGetAsync(MatchMakingServiceRedisKeys.UserMatchHashKey, userId);
            if (matchId.IsNullOrEmpty)
                throw new KeyNotFoundException($"No match found for user with id {userId}");

            var userIds =
                await _db.ListRangeAsync(string.Format(MatchMakingServiceRedisKeys.MatchUsersListKey, matchId));

            var response = new MatchDto(
                matchId.ToString(),
                userIds.Select(u => u.ToString()).ToList()
            );

            return response;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw new ApplicationException("Error while sending matchinfo request", ex);
        }
    }
}