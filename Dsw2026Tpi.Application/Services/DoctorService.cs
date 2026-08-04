using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IPersistence _persistence;
    private readonly IAvailabilityService _availabilityService;


    public DoctorService(IPersistence persistence, IAvailabilityService availabilityService)
    {
        _persistence = persistence;
        _availabilityService = availabilityService;

    }

    public async Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
    {
        var doctors = await _persistence.Paginate<Doctor, string>(
            pageSize,
            pageIndex,
            d => string.IsNullOrWhiteSpace(name) || d.Name.Contains(name),
            x => x.Name,
            nameof(Doctor.Speciality)
        );

        return doctors.Map(d => new DoctorModel.Response(
            d.Id,
            d.Name,
            d.LicenseNumber,
            new DoctorModel.SpecialityDto(d.Speciality?.Id, d.Speciality?.Name)
        ));
    }

    public async Task<DoctorModel.Response> CreateAsync(DoctorModel.Request request)
    {
        ValidateRequest(request);

        var speciality = await _persistence.GetById<Speciality>(request.SpecialityId)
            ?? throw new EntityNotFoundException(nameof(Speciality))
                .WithDetail("specialityId", "La especialidad no existe");

        var doctor = new Doctor(request.Name, request.LicenseNumber, speciality);

        await _persistence.Add(doctor);

        return new DoctorModel.Response(
            doctor.Id,
            doctor.Name,
            doctor.LicenseNumber,
            new DoctorModel.SpecialityDto(speciality.Id, speciality.Name)
        );
    }

    public async Task<DoctorModel.Response> UpdateAsync(Guid id, DoctorModel.Request request)
    {
        ValidateRequest(request);

        var doctor = await _persistence.GetById<Doctor>(id)
            ?? throw new EntityNotFoundException(nameof(Doctor))
                .WithDetail("doctorId", "El médico especificado no existe");

        var speciality = await _persistence.GetById<Speciality>(request.SpecialityId)
            ?? throw new EntityNotFoundException(nameof(Speciality))
                .WithDetail("specialityId", "La especialidad no existe");

        var updatedDoctor = new Doctor(request.Name, request.LicenseNumber, speciality, id);

        await _persistence.Update(updatedDoctor);

        return new DoctorModel.Response(
            updatedDoctor.Id,
            updatedDoctor.Name,
            updatedDoctor.LicenseNumber,
            new DoctorModel.SpecialityDto(speciality.Id, speciality.Name)
        );
    }

    public async Task DeleteAsync(Guid id)
    {
        var doctor = await _persistence.GetById<Doctor>(id)
            ?? throw new EntityNotFoundException(nameof(Doctor))
                .WithDetail("doctorId", "El médico especificado no existe");

        doctor.Deactivate();

        await _persistence.Update(doctor);
    }


    public async Task<IEnumerable<DoctorModel.AvailabilityResponse>> GetAvailabilitiesAsync(Guid id)
    {
        var doctor = await _persistence.GetById<Doctor>(id)
            ?? throw new EntityNotFoundException(nameof(Doctor))
                .WithDetail("doctorId", "El médico especificado no existe");

        var availabilities = await _availabilityService.GetByDoctorAsync(id);

        return availabilities.Select(a =>
            new DoctorModel.AvailabilityResponse(a.Day, a.StartTime, a.EndTime));
    }


    private static void ValidateRequest(DoctorModel.Request request)
    {
        if (request == null)
        {
            throw new ValidationException(
                "Datos inválidos",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("request", "Los datos del médico son obligatorios");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException(
                "Nombre obligatorio",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("name", "El nombre del médico es obligatorio");
        }

        if (request.Name.Length < 3 || request.Name.Length > 100)
        {
            throw new ValidationException(
                "Nombre inválido",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("name", "Debe tener entre 3 y 100 caracteres");
        }

        if (request.SpecialityId == Guid.Empty)
        {
            throw new ValidationException(
                "SpecialityId obligatorio",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("specialityId", "La especialidad es obligatoria");
        }
    }
}