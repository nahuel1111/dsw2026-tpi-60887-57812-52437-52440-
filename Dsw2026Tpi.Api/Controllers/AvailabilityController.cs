using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("availabilities")]
[Authorize(Policy = Policies.AdminPolicy)]
public class AvailabilityController : AppController
{
    private readonly IAvailabilityService _service;

    public AvailabilityController(IAvailabilityService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AvailabilityModel.Request request)
    {
        await _service.CreateMonthAsync(request);
        return Created(string.Empty, null);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] AvailabilityModel.Request request)
    {
        await _service.UpdateMonthAsync(request);
        return Ok();
    }
}
