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


    public async Task<Pagination<SpecialityModel.Response>> GetAll(
        int pageSize,
        int pageIndex,
        string? name = null)
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


        if (!string.IsNullOrWhiteSpace(name) &&
            (name.Length < 3 || name.Length > 100))
        {
            throw new ValidationException(
                "Nombre inválido",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("name", "Debe tener entre 3 y 100 caracteres");
        }


        var specialities = await _persistence.Paginate<Speciality, string>(
            pageSize,
            pageIndex,
            s => s.IsActive &&
                 (string.IsNullOrWhiteSpace(name) ||
                  s.Name.Contains(name)),
            x => x.Name);


        return specialities.Map(s =>
            new SpecialityModel.Response(
                s.Id,
                s.Name,
                s.Description));
    }



    public async Task<SpecialityModel.Response> Create(
        SpecialityModel.Request request)
    {
        ValidateRequest(request);


        var exists = await _persistence.First<Speciality>(
            s => s.Name.ToLower() == request.Name.ToLower()
                 && s.IsActive);


        if (exists != null)
        {
            throw new ConflictException(
                "La especialidad ya existe.",
                ErrorCodes.SPECIALITY_CONFLICT)
                .WithDetail(
                    "name",
                    "Ya existe una especialidad con ese nombre");
        }


        var entity = new Speciality(
            request.Name,
            request.Description);


        var created = await _persistence.Add(entity);


        return new SpecialityModel.Response(
            created.Id,
            created.Name,
            created.Description);
    }



    public async Task<SpecialityModel.Response> Update(
        Guid id,
        SpecialityModel.Request request)
    {
        ValidateRequest(request);


        var existing = await _persistence.GetById<Speciality>(id);


        if (existing is null)
        {
            throw new EntityNotFoundException(nameof(Speciality))
                .WithDetail(
                    "specialityId",
                    "La especialidad no existe");
        }


        var duplicate = await _persistence.First<Speciality>(
            s => s.Name.ToLower() == request.Name.ToLower()
                 && s.Id != id
                 && s.IsActive);


        if (duplicate != null)
        {
            throw new ConflictException(
                "La especialidad ya existe.",
                ErrorCodes.SPECIALITY_CONFLICT)
                .WithDetail(
                    "name",
                    "Ya existe una especialidad con ese nombre");
        }


        var updated = new Speciality(
            request.Name,
            request.Description,
            id);


        await _persistence.Update(updated);


        return new SpecialityModel.Response(
            updated.Id,
            updated.Name,
            updated.Description);
    }



    public async Task Delete(Guid id)
    {
        var existing = await _persistence.GetById<Speciality>(id);


        if (existing is null)
        {
            throw new EntityNotFoundException(nameof(Speciality))
                .WithDetail(
                    "specialityId",
                    "La especialidad no existe");
        }


        existing.SoftDelete();


        await _persistence.Update(existing);
    }



    private static void ValidateRequest(
        SpecialityModel.Request request)
    {
        if (request == null)
        {
            throw new ValidationException(
                "Datos inválidos",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "request",
                    "Los datos de la especialidad son obligatorios");
        }


        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException(
                "Nombre obligatorio",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "name",
                    "El nombre de la especialidad es obligatorio");
        }


        if (request.Name.Length < 3 ||
            request.Name.Length > 100)
        {
            throw new ValidationException(
                "Nombre inválido",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "name",
                    "Debe tener entre 3 y 100 caracteres");
        }


        if (!string.IsNullOrWhiteSpace(request.Description) &&
            request.Description.Length > 500)
        {
            throw new ValidationException(
                "Descripción inválida",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "description",
                    "No puede superar los 500 caracteres");
        }
    }
}