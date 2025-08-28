using System.Text.Json;
using Confluent.Kafka;
using MatchMaking.Common.Constants;
using MatchMaking.Common.Messages;
using MatchMaking.Service.BL.Abstraction.Services;
using MatchMaking.Service.BL.Constants;
using MatchMaking.Service.BL.Models;
using MatchMaking.Service.DAL.Abstraction.Repositories;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MatchMaking.Service.BL.Services;

public class MatchMakingService(
    ILogger<MatchMakingService> logger,
    IProducer<Null, MatchMakingRequestMessage> producer,
    IServiceRepository serviceRepository)
    : IMatchMakingService
{
    public async Task SearchMatch(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new InvalidOperationException("UserId is required.");

            var isWaiting = await serviceRepository.CheckUserIsWaiting(userId);
            if (isWaiting)
                throw new InvalidOperationException("User has already sent a request and waiting for a match");

            await producer.ProduceAsync(KafkaTopics.KafkaRequestTopic,
                new Message<Null, MatchMakingRequestMessage> { Value = new MatchMakingRequestMessage(userId) });

            //Register user as waiting user to not allow to request for a new match,
            //while the match is not yet constructed 
            await serviceRepository.RegisterWaitingUser(userId);
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

            var matchId = await serviceRepository.FindMatchIdByUserId(userId);
            if (matchId == null)
                throw new KeyNotFoundException($"No match found for user with id {userId}");

            var userIds = await serviceRepository.GetMatchDetails(matchId);

            var response = new MatchDto(
                matchId,
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
            logger.LogError(ex, ex.Message);
            throw new ApplicationException("Error while sending matchinfo request", ex);
        }
    }
}