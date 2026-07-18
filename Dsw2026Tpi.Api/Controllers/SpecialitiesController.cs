using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Dsw2026Tpi.Api.Controllers;

[Route("api/specialties")]
[Authorize(Policy = Policies.AdminPolicy)]

    public class SpecialitiesController : AppController
{
    private readonly ISpecialityService _service;

    public SpecialitiesController(ISpecialityService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageSize, [FromQuery] int pageIndex, [FromQuery] string? name = null)
    {
        var items = await _service.GetAll(pageSize, pageIndex, name);
        return Ok(items);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] SpecialityModel.Request request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var created = await _service.Create(request);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Put(Guid id, [FromBody] SpecialityModel.Request request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var updated = await _service.Update(id, request);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.Delete(id);
        return NoContent();
    }
}

