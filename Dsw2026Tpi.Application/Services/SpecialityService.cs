using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class SpecialityService : ISpecialityService
{
    private readonly IPersistence _persistence;

    public SpecialityService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<Pagination<SpecialityModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
    {


        if (pageSize <= 0)
        {
            throw new ValidationException(
                "PageSize inválido",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("pageSize", "Debe ser mayor a 0");
        }

        if (pageIndex < 0)
        {
            throw new ValidationException(
                "PageIndex inválido",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("pageIndex", "Debe ser mayor o igual a 0");
        }

        if (!string.IsNullOrWhiteSpace(name) && (name.Length < 3 || name.Length > 100))
        {
            throw new ValidationException(
                "Nombre inválido",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("name", "Debe tener entre 3 y 100 caracteres");
        }
        var specialities = await _persistence.Paginate<Speciality, string>(
            pageSize,
            pageIndex,
            s => s.IsActive && (string.IsNullOrWhiteSpace(name) || s.Name.Contains(name)),
            x => x.Name
        );

        return specialities.Map(s => new SpecialityModel.Response(
            s.Id,
            s.Name,
            s.Description
        ));
    }

    public async Task<SpecialityModel.Response> Create(SpecialityModel.Request request)
    {
        var entity = new Speciality(request.Name, request.Description);
        var created = await _persistence.Add(entity);
        return new SpecialityModel.Response(created.Id, created.Name, created.Description);
    }

    public async Task<SpecialityModel.Response> Update(Guid id, SpecialityModel.Request request)
    {
        var existing = await _persistence.GetById<Speciality>(id);
        if (existing is null)
            throw new EntityNotFoundException(nameof(Speciality));

        var updated = new Speciality(request.Name, request.Description, id);
        await _persistence.Update(updated);
        return new SpecialityModel.Response(updated.Id, updated.Name, updated.Description);
    }

    public async Task Delete(Guid id)
    {
        var existing = await _persistence.GetById<Speciality>(id);

        if (existing is null)
            throw new EntityNotFoundException(nameof(Speciality));

        existing.SoftDelete();

        await _persistence.Update(existing);
    }
}