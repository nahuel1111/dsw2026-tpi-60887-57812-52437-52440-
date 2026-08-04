using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IPersistence _persistence;

    public DoctorService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
    {
        // Validar el filtro de búsqueda por nombre si se proporciona (longitud entre 3 y 100)
        if (!string.IsNullOrWhiteSpace(name))
        {
            var trimmedName = name.Trim();
            if (trimmedName.Length < 3 || trimmedName.Length > 100)
            {
                throw new ValidationException(
                    ErrorCodes.VALIDATION_ERROR,
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail("name", "longitud_invalida_3_100");
            }
        }

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
        // Validar los datos de entrada
        ValidateDoctorRequest(request);

        // Validar que la especialidad exista y esté activa
        var speciality = await _persistence.GetById<Speciality>(request.SpecialityId)
            ?? throw new ValidationException(
                ErrorCodes.ENTITY_NOTFOUND,
                nameof(ErrorCodes.ENTITY_NOTFOUND))
                .WithDetail("specialityId", "especialidad_no_encontrada");

        // Crear la entidad Doctor
        var doctor = new Doctor(request.Name.Trim(), request.LicenseNumber, speciality);

        // Guardar en la base de datos
        await _persistence.Add(doctor);

        // Retornar la respuesta mapeada
        return new DoctorModel.Response(
            doctor.Id,
            doctor.Name,
            doctor.LicenseNumber,
            new DoctorModel.SpecialityDto(speciality.Id, speciality.Name)
        );
    }

    public async Task<DoctorModel.Response> UpdateAsync(Guid id, DoctorModel.Request request)
    {
        // Validar datos de entrada
        ValidateDoctorRequest(request);

        // Buscar el médico existente
        var doctor = await _persistence.GetById<Doctor>(id)
            ?? throw new ValidationException(
                ErrorCodes.ENTITY_NOTFOUND,
                nameof(ErrorCodes.ENTITY_NOTFOUND))
                .WithDetail("id", "medico_no_encontrado");

        // Validar que la especialidad exista
        var speciality = await _persistence.GetById<Speciality>(request.SpecialityId)
            ?? throw new ValidationException(
                ErrorCodes.ENTITY_NOTFOUND,
                nameof(ErrorCodes.ENTITY_NOTFOUND))
                .WithDetail("specialityId", "especialidad_no_encontrada");

        // Actualizar la entidad
        var updatedDoctor = new Doctor(request.Name.Trim(), request.LicenseNumber, speciality, id);
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
        // Buscar el médico existente
        var doctor = await _persistence.GetById<Doctor>(id)
            ?? throw new ValidationException(
                ErrorCodes.ENTITY_NOTFOUND,
                nameof(ErrorCodes.ENTITY_NOTFOUND))
                .WithDetail("id", "medico_no_encontrado");

        // Aplicar borrado lógico
        doctor.Deactivate();

        // Actualizar en la base de datos
        await _persistence.Update(doctor);
    }

    public async Task<IEnumerable<DoctorModel.AvailabilityResponse>> GetAvailabilitiesAsync(Guid id)
    {
        // Validar que el médico exista en la base de datos
        var doctor = await _persistence.GetById<Doctor>(id)
            ?? throw new ValidationException(
                ErrorCodes.ENTITY_NOTFOUND,
                nameof(ErrorCodes.ENTITY_NOTFOUND))
                .WithDetail("id", "medico_no_encontrado");

        // Si no hay disponibilidades cargadas aún, retornamos lista vacía según la especificación
        return new List<DoctorModel.AvailabilityResponse>();
    }

    /// <summary>
    /// Método privado para validar reglas de negocio sobre la solicitud de un médico.
    /// </summary>
    private static void ValidateDoctorRequest(DoctorModel.Request request)
    {
        if (request == null)
        {
            throw new ValidationException(
                ErrorCodes.VALIDATION_ERROR,
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("request", "solicitud_nula");
        }

        // Validar Nombre obligatorio
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException(
                ErrorCodes.VALIDATION_ERROR,
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("name", "nombre_requerido");
        }

        var trimmedName = request.Name.Trim();

        // Validar longitud del Nombre (entre 3 y 100 caracteres)
        if (trimmedName.Length < 3 || trimmedName.Length > 100)
        {
            throw new ValidationException(
                ErrorCodes.VALIDATION_ERROR,
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("name", "longitud_invalida_3_100");
        }

        // Validar Especialidad ID no vacía
        if (request.SpecialityId == Guid.Empty)
        {
            throw new ValidationException(
                ErrorCodes.VALIDATION_ERROR,
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("specialityId", "especialidad_requerida");
        }
    }
}