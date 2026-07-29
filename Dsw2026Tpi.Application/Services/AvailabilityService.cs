using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using System.Linq;

namespace Dsw2026Tpi.Application.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly IPersistence _persistence;

    public AvailabilityService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<IEnumerable<AvailabilityModel.Response>> GetByDoctorAsync(Guid doctorId)
    {
        var doctor = await _persistence.GetById<Doctor>(doctorId);
        if (doctor == null) throw new EntityNotFoundException(nameof(Doctor)).WithDetail("doctor", "doctor_no_encontrado");

        var now = DateTime.UtcNow;
        var startMonth = new DateTime(now.Year, now.Month, 1);
        var endMonth = startMonth.AddMonths(1);

        var slots = await _persistence.GetFiltered<Availability>(a => a.DoctorId == doctorId && a.Start >= startMonth && a.Start < endMonth);

        var grouped = slots?.GroupBy(s => s.Start.DayOfWeek).ToDictionary(g => g.Key, g => g.OrderBy(x => x.Start).ToList())
            ?? new Dictionary<DayOfWeek, List<Availability>>();

        var result = new List<AvailabilityModel.Response>();

        foreach (DayOfWeek dow in Enum.GetValues(typeof(DayOfWeek)))
        {
            if (!grouped.ContainsKey(dow) || grouped[dow].Count == 0) continue;
            var daySlots = grouped[dow];
            var start = daySlots.First().Start.TimeOfDay;
            var end = daySlots.Last().Start.AddMinutes(30).TimeOfDay;
            result.Add(new AvailabilityModel.Response(dow.ToString(), start.ToString(@"hh\:mm"), end.ToString(@"hh\:mm")));
        }

        return result;
    }

    public Task CreateMonthAsync(AvailabilityModel.Request request)
    {
        return CreateOrUpdateMonthInternalAsync(request, isUpdate: false);
    }

    public Task UpdateMonthAsync(AvailabilityModel.Request request)
    {
        return CreateOrUpdateMonthInternalAsync(request, isUpdate: true);
    }

    private async Task CreateOrUpdateMonthInternalAsync(AvailabilityModel.Request request, bool isUpdate)
    {
        var doctor = await _persistence.GetById<Doctor>(request.DoctorId);
        if (doctor == null) throw new EntityNotFoundException(nameof(Doctor)).WithDetail("doctor", "doctor_no_encontrado");

        if (request.Days == null || !request.Days.Any())
            throw new ValidationException(
    ErrorCodes.DAYS_REQUIRED,
    nameof(ErrorCodes.VALIDATION_ERROR))
    .WithDetail("dias", "debe_indicar_al_menos_un_dia");

        var parsed = new List<(DayOfWeek Day, TimeSpan Start, TimeSpan End)>();
        foreach (var d in request.Days)
        {
            if (!Enum.TryParse<DayOfWeek>(d.Day, true, out var dow))
                throw new ValidationException(
     ErrorCodes.INVALID_DAY,
     nameof(ErrorCodes.VALIDATION_ERROR))
     .WithDetail("dia", "dia_invalido");
            if (!TimeSpan.TryParse(d.StartTime, out var st) || !TimeSpan.TryParse(d.EndTime, out var et))
                throw new ValidationException(
      ErrorCodes.INVALID_TIME_FORMAT,
      nameof(ErrorCodes.VALIDATION_ERROR))
      .WithDetail("hora", "formato_hora_invalido");
            if (st >= et) throw new ValidationException(
    ErrorCodes.START_TIME_AFTER_END_TIME,
    nameof(ErrorCodes.VALIDATION_ERROR))
    .WithDetail("hora", "hora_inicio_debe_ser_menor_que_hora_fin");
            parsed.Add((dow, st, et));
        }

        var now = DateTime.UtcNow;
        var startMonth = new DateTime(now.Year, now.Month, 1);
        var endMonth = startMonth.AddMonths(1);

        if (isUpdate)
        {
            var existing = await _persistence.GetFiltered<Availability>(a => a.DoctorId == request.DoctorId && a.Start >= startMonth && a.Start < endMonth);
            if (existing != null)
            {
                foreach (var e in existing)
                {
                    await _persistence.Delete(e);
                }
            }
        }

        var toCreate = new List<Availability>();

        for (var day = startMonth; day < endMonth; day = day.AddDays(1))
        {
            var dow = day.DayOfWeek;
            var ranges = parsed.Where(p => p.Day == dow);

            foreach (var r in ranges)
            {
                var current = day.Date + r.Start;
                var end = day.Date + r.End;

                while (current < end)
                {
                    toCreate.Add(new Availability(request.DoctorId, current));
                    current = current.AddMinutes(30);
                }
            }
        }

        var duplicates = toCreate
            .GroupBy(t => new { t.DoctorId, t.Start })
            .Any(g => g.Count() > 1);

        if (duplicates)
        {
            throw new ConflictException(
                nameof(ErrorCodes.AVAILABILITY_CONFLICT),
                ErrorCodes.AVAILABILITY_CONFLICT)
                .WithDetail("horario", "horario_solapado");
        }

        if (!isUpdate)
        {
            var existing = await _persistence.GetFiltered<Availability>(
                a => a.DoctorId == request.DoctorId &&
                     a.Start >= startMonth &&
                     a.Start < endMonth);

            if (existing != null && existing.Any())
            {
                var overlap = existing.Any(e =>
                    toCreate.Any(t => t.Start == e.Start));

                if (overlap)
                {
                    throw new ConflictException(
                        nameof(ErrorCodes.AVAILABILITY_CONFLICT),
                        ErrorCodes.AVAILABILITY_CONFLICT)
                        .WithDetail("horario", "horario_solapado");
                }
            }
        }

        foreach (var slot in toCreate)
        {
            await _persistence.Add(slot);
        }
    }
}
