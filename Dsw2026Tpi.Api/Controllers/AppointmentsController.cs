using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/appointments")]
public class AppointmentsController : AppController
{
    private readonly IAppointmentService _service;

    public AppointmentsController(IAppointmentService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Policy = Policies.PatientPolicy)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] AppointmentModel.Request request)
    {
        var created = await _service.CreateAsync(request);
        return Created(string.Empty, created);
    }

    [HttpGet("patient")]
    [Authorize(Policy = Policies.PatientPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPatient([FromQuery] long dni)
    {
        var items = await _service.GetByPatientAsync(dni);
        return Ok(items);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.PatientPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel([FromRoute] Guid id)
    {
        await _service.CancelAsync(id);
        return NoContent();
    }

    [HttpGet]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDate([FromQuery] DateTime date)
    {
        var items = await _service.GetByDateAsync(date);
        return Ok(items);
    }

    [HttpGet("search")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? specialtyId,
        [FromQuery] Guid? doctorId,
        [FromQuery] long? dni,
        [FromQuery] DateTime? date,
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 1)
    {
        var res = await _service.SearchAsync(
            specialtyId,
            doctorId,
            dni,
            date,
            pageSize,
        pageIndex);
        return Ok(res);
    }
}