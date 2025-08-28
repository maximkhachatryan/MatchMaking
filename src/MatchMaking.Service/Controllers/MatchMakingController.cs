using Confluent.Kafka;
using MatchMaking.Service.BL.Abstraction.Services;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace MatchMaking.Service.Controllers;

[ApiController]
[Route("api/matchmaking")]
public class MatchMakingController : ControllerBase
{
    private readonly IMatchMakingService _matchMakingService;

    public MatchMakingController(IMatchMakingService matchMakingService)
    {
        _matchMakingService = matchMakingService;
    }

    [HttpPost("search/{userId}")]
    public async Task<IActionResult> MatchSearchRequest([FromRoute] string userId)
    {
        //TODO: Would be better to implement custom exception handling middleware.
        try
        {
            await _matchMakingService.SearchMatch(userId);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ApplicationException ex)
        {
            return StatusCode(500, $"Error sending message: {ex.Message}");
        }
        return NoContent();
    }

    [HttpGet("matchinfo/userId/{userId}")]
    public async Task<IActionResult> RetrieveMatchInformation([FromRoute] string userId)
    {
        try
        {
            var response = await _matchMakingService.RetrieveMatchInformation(userId);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ApplicationException ex)
        {
            return StatusCode(500, $"Error sending message: {ex.Message}");
        }
    }
}