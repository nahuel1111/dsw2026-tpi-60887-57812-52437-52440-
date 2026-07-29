using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

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
        // 1. Validar que la especialidad exista
        var speciality = await _persistence.GetById<Speciality>(request.SpecialityId)
            ?? throw new Exception("La especialidad especificada no existe.");

        // 2. Crear la entidad Doctor
        var doctor = new Doctor(request.Name, request.LicenseNumber, speciality);

        // 3. Guardar en la base de datos
        await _persistence.Add(doctor);

        // 4. Retornar la respuesta
        return new DoctorModel.Response(
            doctor.Id,
            doctor.Name,
            doctor.LicenseNumber,
            new DoctorModel.SpecialityDto(speciality.Id, speciality.Name)
        );
    }

    public async Task<DoctorModel.Response> UpdateAsync(Guid id, DoctorModel.Request request)
    {
        // 1. Buscar el médico existente
        var doctor = await _persistence.GetById<Doctor>(id)
            ?? throw new Exception("El médico especificado no existe.");

        // 2. Validar que la especialidad exista
        var speciality = await _persistence.GetById<Speciality>(request.SpecialityId)
            ?? throw new Exception("La especialidad especificada no existe.");

        // 3. Actualizar entidad
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
        // 1. Buscar el médico existente
        var doctor = await _persistence.GetById<Doctor>(id)
            ?? throw new Exception("El médico especificado no existe.");

        // 2. Aplicar borrado lógico
        doctor.Deactivate();

        // 3. Actualizar en la base de datos
        await _persistence.Update(doctor);
    }


    public async Task<IEnumerable<DoctorModel.AvailabilityResponse>> GetAvailabilitiesAsync(Guid id)
    {
        var doctor = await _persistence.GetById<Doctor>(id)
            ?? throw new Exception("El médico especificado no existe.");

        var availabilities = await _availabilityService.GetByDoctorAsync(id);

        return availabilities.Select(a =>
            new DoctorModel.AvailabilityResponse(a.Day, a.StartTime, a.EndTime));
    }
}