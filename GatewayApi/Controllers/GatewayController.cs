// GatewayController.cs

using Microsoft.AspNetCore.Mvc;
using RaftElection;

[ApiController]
[Route("[controller]")]
public class GatewayController : ControllerBase
{
    private readonly Gateway _gateway;

    public GatewayController(Gateway gateway)
    {
        _gateway = gateway;

    }

    [HttpGet("EventualGet/{key}")]
    public async Task<(int?, int?)> EventualGet(string key)
    {
        return await _gateway.EventualGetAsync(key);
    }

    [HttpGet("strongGet/{key}")]
    public async Task<(int?, int?)> StrongGet(string key)
    {
        return await _gateway.StrongGetAsync(key);
    }

    [HttpPost("compareandswap")]
    public async Task<IActionResult> CompareandSwap(SwapInfo swap)
    {
        var response = await _gateway.CompareVersionAndSwapAsync(swap);
        if (response)
        {
            return Ok();
        }
        else
        { return StatusCode(500, response.ToString()); }
    }

    [HttpPost("newValue")]
    public async Task AddNewValue(KeyValue pair)
    {
        await _gateway.WriteAsync(pair.key, pair.value);
    }

    //[HttpGet("userBalance")]
    //public async Task UserBalance(string username)
    //{
    //    await _gateway.
    //}
}
