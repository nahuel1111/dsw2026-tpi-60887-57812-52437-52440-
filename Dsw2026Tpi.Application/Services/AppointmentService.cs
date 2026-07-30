using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Dsw2026Tpi.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IPersistence _persistence;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        IPersistence persistence,
        ILogger<AppointmentService> logger)
    {
        _persistence = persistence;
        _logger = logger;
    }


    public async Task<AppointmentModel.Response> CreateAsync(AppointmentModel.Request request)
    {
        if (request.AvailabilityId == Guid.Empty)
            throw new ValidationException(
                "La disponibilidad es obligatoria.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("availabilityId", "La disponibilidad es obligatoria.");

        if (request.Patient == null || request.Patient.dni == 0)
            throw new ValidationException(
                "El DNI es obligatorio.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("dni", "El DNI es obligatorio.");

        var dniStr = request.Patient.dni.ToString();

        if (dniStr.Length < 7 || dniStr.Length > 10)
            throw new ValidationException(
                "El DNI debe tener entre 7 y 10 dígitos.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("dni", "El DNI debe tener entre 7 y 10 dígitos.");

        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < 5)
            throw new ValidationException(
                "El motivo debe tener al menos 5 caracteres.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("reason", "El motivo debe tener al menos 5 caracteres.");

        var doctor = await _persistence.GetById<Doctor>(request.DoctorId);

        if (doctor == null)
            throw new EntityNotFoundException(nameof(Doctor))
                .WithDetail("doctorId", "El doctor no existe.");

        var availability = await _persistence.GetById<Availability>(request.AvailabilityId);

        if (availability == null)
            throw new EntityNotFoundException(nameof(Availability))
                .WithDetail("availabilityId", "La disponibilidad no existe.");

        if (availability.DoctorId != request.DoctorId)
            throw new ValidationException(
                "La disponibilidad no corresponde al doctor.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("availabilityId", "La disponibilidad no corresponde al doctor.");

        if (availability.Start < DateTime.UtcNow)
            throw new ValidationException(
                "No se pueden reservar turnos en el pasado.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("dateTime", "No se pueden reservar turnos en el pasado.");

        var existing = await _persistence.First<Appointment>(
            a => a.AvailabilityId == request.AvailabilityId &&
                 a.State == AppointmentState.BOOKED);

        if (existing != null)
            throw new ConflictException(
                "El turno ya fue reservado.",
                ErrorCodes.AVAILABILITY_CONFLICT)
                .WithDetail("availabilityId", "El turno ya fue reservado.");

        var appointment = new Appointment(
            request.DoctorId,
            request.AvailabilityId,
            request.Patient.dni,
            request.Reason);

        await _persistence.Add(appointment);


        _logger.LogInformation(
            "Se creó un turno para el paciente {Dni} con el doctor {DoctorId} en la disponibilidad {AvailabilityId}",
            appointment.PatientDni,
            appointment.DoctorId,
            appointment.AvailabilityId);


        return new AppointmentModel.Response(
            appointment.Id,
            appointment.DoctorId,
            appointment.AvailabilityId,
            appointment.PatientDni,
            appointment.Reason,
            appointment.State.ToString(),
            appointment.CreatedAt,
            appointment.UpdatedAt);
    }


    public async Task<IEnumerable<AppointmentModel.Response>> GetByPatientAsync(long dni)
    {
        var results = await _persistence.GetFiltered<Appointment>(
            a => a.PatientDni == dni &&
                 a.State == AppointmentState.BOOKED);

        return (results ?? Enumerable.Empty<Appointment>())
            .Select(a => new AppointmentModel.Response(
                a.Id,
                a.DoctorId,
                a.AvailabilityId,
                a.PatientDni,
                a.Reason,
                a.State.ToString(),
                a.CreatedAt,
                a.UpdatedAt));
    }


    public async Task CancelAsync(Guid id)
    {
        var appointment = await _persistence.GetById<Appointment>(id);

        if (appointment == null)
            throw new EntityNotFoundException(nameof(Appointment))
                .WithDetail("appointmentId", "El turno no existe.");

        if (appointment.State != AppointmentState.BOOKED)
            throw new ConflictException(
                "Solo se pueden cancelar turnos reservados.",
                ErrorCodes.AVAILABILITY_CONFLICT)
                .WithDetail("state", "Solo se pueden cancelar turnos en estado BOOKED.");

        appointment.Cancel();

        await _persistence.Update(appointment);


        _logger.LogInformation(
            "Se canceló el turno {AppointmentId}",
            appointment.Id);
    }


    public async Task<IEnumerable<AppointmentModel.SearchResult>> GetByDateAsync(DateTime date)
    {
        _logger.LogInformation(
            "Se realizó búsqueda de turnos por fecha {Date}",
            date.Date);


        var start = date.Date;
        var end = start.AddDays(1);

        var appointments = await _persistence.GetFiltered<Appointment>(
            a => a.State == AppointmentState.BOOKED);

        var res = new List<AppointmentModel.SearchResult>();

        if (appointments == null)
            return res;

        foreach (var a in appointments)
        {
            var avail = await _persistence.GetById<Availability>(a.AvailabilityId);

            if (avail == null)
                continue;

            if (avail.Start < start || avail.Start >= end)
                continue;

            var doctor = await _persistence.GetById<Doctor>(
                a.DoctorId,
                nameof(Doctor.Speciality));

            var specialty = doctor?.Speciality?.Name ?? string.Empty;
            var doctorName = doctor?.Name ?? string.Empty;

            res.Add(
                new AppointmentModel.SearchResult(
                    specialty,
                    doctorName,
                    avail.Start,
                    a.DoctorId,
                    a.AvailabilityId));
        }

        return res;
    }


    public async Task<Pagination<AppointmentModel.SearchResult>> SearchAsync(
        Guid? specialtyId,
        Guid? doctorId,
        long? dni,
        DateTime? date,
        int pageSize,
        int pageIndex)
    {

        _logger.LogInformation(
            "Se realizó búsqueda combinada de turnos. SpecialtyId: {SpecialtyId}, DoctorId: {DoctorId}, DNI: {Dni}, Fecha: {Date}",
            specialtyId,
            doctorId,
            dni,
            date);


        var all = await _persistence.GetFiltered<Appointment>(
            a => a.State == AppointmentState.BOOKED);

        var list = (all ?? Enumerable.Empty<Appointment>()).ToList();

        var filtered = new List<AppointmentModel.SearchResult>();

        foreach (var a in list)
        {
            var doctor = await _persistence.GetById<Doctor>(
                a.DoctorId,
                nameof(Doctor.Speciality));

            if (doctor == null)
                continue;

            if (specialtyId.HasValue &&
                doctor.SpecialityId != specialtyId)
                continue;

            if (doctorId.HasValue &&
                a.DoctorId != doctorId)
                continue;

            if (dni.HasValue &&
                a.PatientDni != dni)
                continue;

            var avail = await _persistence.GetById<Availability>(
                a.AvailabilityId);

            if (avail == null)
                continue;

            if (date.HasValue)
            {
                var d = date.Value.Date;

                if (avail.Start.Date != d)
                    continue;
            }

            filtered.Add(
                new AppointmentModel.SearchResult(
                    doctor.Speciality?.Name ?? string.Empty,
                    doctor.Name,
                    avail.Start,
                    doctor.Id,
                    a.AvailabilityId));
        }

        var total = filtered.Count;

        var pageSizeAbs = Math.Abs(pageSize);

        var pageIndexNormalized =
            Math.Abs(pageIndex) == 0
                ? 0
                : Math.Abs(pageIndex) - 1;

        var pageData = filtered
            .OrderBy(r => r.AvailableTime)
            .Skip(pageIndexNormalized * pageSizeAbs)
            .Take(pageSizeAbs);


        return new Pagination<AppointmentModel.SearchResult>(
            pageSizeAbs,
            pageIndexNormalized,
            total,
            pageData);
    }
}